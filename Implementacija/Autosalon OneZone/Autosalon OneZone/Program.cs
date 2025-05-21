// Put this code in your Program.cs file at the root of your project

using Autosalon_OneZone.Models; // Namespace gde su vaši entiteti (ApplicationUser, itd.)
using Autosalon_OneZone.Services; // Namespace gde su vaši servisi (IVoziloService, VoziloService, itd.)
using Microsoft.AspNetCore.Identity; // Potrebno za Identity, UserManager, RoleManager, IdentityRole
using Microsoft.EntityFrameworkCore; // Potrebno za UseSqlServer
using Autosalon_OneZone.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Konfiguracija Servisa (Add services to the container.) ---

// 1. Konfiguracija AppDbContext-a
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Dodaje podršku za EF Core Database Developer Page Exception Filter (korisno u Development okruženju)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. Konfiguracija ASP.NET Core Identity
// Koristi ApplicationUser kao tip korisnika i IdentityRole kao tip role
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Konfiguracija Identity opcija (Password, Lockout, User, SignIn)
    // Ove opcije ste ranije naveli, prilagodite ih svojim zahtevima
    options.SignIn.RequireConfirmedAccount = false; // Promenite ako želite potvrdu naloga emailom
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false; // Primer: Ne zahteva specijalne karaktere
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true; // Email adresa mora biti jedinstvena
                                            // ostale opcije...
})
    // Povezuje Identity sa vašim AppDbContext-om za skladištenje podataka
    .AddEntityFrameworkStores<ApplicationDbContext>()
    // Dodaje standardne token provajdere (za reset lozinke, potvrdu emaila itd.)
    .AddDefaultTokenProviders();

// 3. Konfiguracija Authorization Policy-ja baziranih na Rolama
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireProdavacRole", policy => policy.RequireRole("Prodavac"));
    options.AddPolicy("RequireKupacRole", policy => policy.RequireRole("Kupac"));
    // Možete dodati i druge policy-je ovde
});

// 4. Registracija vaših prilago?enih servisa
// Registracija IVoziloService i njegove implementacije VoziloService
builder.Services.AddScoped<IVoziloService, VoziloService>();

// Registrujte i ostale vaše servise ovde kako ih budete kreirali
// builder.Services.AddScoped<IKorpaService, KorpaService>();
// builder.Services.AddScoped<INarudzbaService, NarudzbaService>();
// builder.Services.AddScoped<IEmailSender, EmailSender>(); // Registrujte EmailSender ako ste ga kreirali


// Dodavanje podrške za Kontrolere sa View-ovima (standardno za MVC)
builder.Services.AddControllersWithViews();

// Dodavanje podrške za Razor Pages (koristi se podrazumevano sa Identity UI)
builder.Services.AddRazorPages();


var app = builder.Build();

// --- NOVO DODANO: Logika za inicijalizaciju Identity podataka (Role i Admin Korisnik) pri pokretanju ---
// Ova sekcija se izvršava NAKON što je aplikacija izgrađena (builder.Build()) ali PRE nego što počne da sluša zahteve (app.Run()).

// Kreira se servis scope da bi se bezbedno pristupilo servisima (UserManager, RoleManager, IConfiguration) konfigurisanim u ConfigureServices
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        // Dohvati potrebne servise iz serviceProvider-a
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>(); // Dohvati IConfiguration servis
        // Možeš dohvatiti i logger ako ti treba za logovanje grešaka pri seedingu
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); // Dohvati logger

        // --- ČITANJE PODATAKA ZA ADMIN KORISNIKA IZ KONFIGURACIJE (appsettings.json) ---
        string adminRoleName = "Administrator"; // Ime Admin role (ostaje konstantno)
        // Čitaj email i lozinku iz "AdminUserSecrets" sekcije u konfiguraciji
        string adminEmail = configuration["AdminUserSecrets:Email"]; // Čitaj email
        string adminPassword = configuration["AdminUserSecrets:Password"]; // Čitaj lozinku
        // <<< DODANO: Čitaj Ime i Prezime iz konfiguracije >>>
        // Koristi ?? "" da osiguraš da vrednost nije null, jer baza to ne dozvoljava
        string adminFirstName = configuration["AdminUserSecrets:FirstName"] ?? ""; // Čitaj Ime, ako ne nađe stavi ""
        string adminLastName = configuration["AdminUserSecrets:LastName"] ?? "";   // Čitaj Prezime, ako ne nađe stavi ""
        // -------------------------------------------------------------------

        // Dodaj osnovnu proveru da li su REQUIRED podaci (Email, Password) pronađeni u konfiguraciji
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("UPOZORENJE: Admin korisnički Email ili Lozinka nisu pronađeni u konfiguraciji ('AdminUserSecrets' sekcija u appsettings.json). Admin korisnik možda neće biti kreiran.");
            // Ispiši i na konzolu
            Console.WriteLine("UPOZORENJE: Admin korisnički Email ili Lozinka nisu pronađeni u konfiguraciji ('AdminUserSecrets' sekcija u appsettings.json). Admin korisnik možda neće biti kreiran.");
            // Možeš i vratiti se (return) iz ovog bloka ili baciti izuzetak ako je admin strogo required
        }
        else // Nastavi samo ako su REQUIRED podaci pronađeni
        {
            // 1. Proveri da li Admin Rola postoji. Ako ne postoji, kreiraj je.
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRoleName));
                logger.LogInformation($"Rola '{adminRoleName}' kreirana.");
            }

            // 2. Proveri da li Admin Korisnik postoji (po email adresi).
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            // 3. Ako Admin Korisnik ne postoji, kreiraj ga.
            if (adminUser == null)
            {
                // Kreiraj novu ApplicationUser instancu sa definisanim podacima
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail, // Korisničko ime
                    Email = adminEmail,
                    EmailConfirmed = true, // Pretpostavljamo da je admin email potvrđen
                    // <<< DODANO: Postavi Ime i Prezime iz pročitanih vrednosti (ili default "") >>>
                    Ime = adminFirstName, // Dodeljuje pročitano Ime (ili "")
                    Prezime = adminLastName // Dodeljuje pročitano Prezime (ili "")
                    // Dodaj ostala svojstva ako su potrebna
                };

                // Kreiraj korisnika sa pročitanom lozinkom
                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

                // Proveri da li je kreiranje bilo uspešno
                if (createAdminResult.Succeeded)
                {
                    // 4. Ako je korisnik uspešno kreiran, dodeli mu Admin rolu.
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                    logger.LogInformation($"Admin korisnik '{adminEmail}' kreiran i dodeljena mu je rola '{adminRoleName}'.");
                }
                else
                {
                    // Ako kreiranje korisnika nije uspelo (npr. lozinka preslaba, email već postoji ako nije proveren ranije), loguj greške
                    Console.WriteLine($"Greška pri kreiranju admin korisnika '{adminEmail}': {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                    logger.LogError($"Failed to create admin user '{adminEmail}': {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                }
            }
            else // Ako admin korisnik već postoji
            {
                // 5. Proveri da li već ima Admin rolu. Ako nema, dodeli mu je.
                if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
                {
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                    logger.LogInformation($"Admin korisnik '{adminEmail}' već postoji, dodeljena mu je rola '{adminRoleName}'.");
                }

                // --- Opciono: Ažuriraj Ime/Prezime ako se promene u konfiguraciji ---
                // Ovo je složenije i zahteva proveru da li su se promenili pre update-a
                // Ako želiš da automatski ažuriraš Ime i Prezime admina ako ih promeniš u appsettings.json
                // if (adminUser.Ime != adminFirstName || adminUser.Prezime != adminLastName) {
                //    adminUser.Ime = adminFirstName;
                //    adminUser.Prezime = adminLastName;
                //    var updateResult = await userManager.UpdateAsync(adminUser);
                //    if(updateResult.Succeeded) {
                //        logger.LogInformation($"Ažurirano ime/prezime za admin korisnika '{adminEmail}'.");
                //    } else {
                //        logger.LogError($"Greška pri ažuriranju imena/prezimena za admin korisnika '{adminEmail}': {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                //    }
                // }
                // -------------------------------------------------------------------
            }
        } // Kraj bloka ako su required podaci pronađeni u konfiguraciji
    }
    catch (Exception ex)
    {
        // Ako dođe do greške tokom inicijalizacije, loguj je.
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Došlo je do greške prilikom inicijalizacije Identity podataka (rola i admin korisnika).");
        // Opciono: Možeš odlučiti da li ćeš zaustaviti pokretanje aplikacije ili nastaviti.
        // throw; // Baci izuzetak dalje ako želiš da se aplikacija sruši pri grešci inicijalizacije
    }
}
// --- KRAJ NOVO DODANE Logike za inicijalizaciju ---


// --- Konfiguracija HTTP Request Pipeline-a (Configure the HTTP request pipeline.) ---

// Konfiguracija za razvojno okruženje
if (app.Environment.IsDevelopment())
{
    // Prikazuje detaljne greške baze podataka u developmentu
    app.UseMigrationsEndPoint();
}
else
{
    // Middleware za globalno upravljanje greškama u produkciji
    app.UseExceptionHandler("/Home/Error");
    // Dodaje HSTS (HTTP Strict Transport Security) header za sigurnost
    app.UseHsts();
}

// Preusmerava HTTP zahteve na HTTPS
app.UseHttpsRedirection();


app.UseStaticFiles();

// Middleware za rutiranje - mapira URL-ove zahteva na endpoint-e (kontrolere, Razor Pages)
app.UseRouting();

// Dodaje autentifikacioni middleware
app.UseAuthentication();
// Dodaje autorizacioni middleware (treba do?i NAKON autentifikacije)
app.UseAuthorization();

// Mapira kontroler endpoint-e
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapira Razor Pages endpoint-e (potrebno za Identity UI)
app.MapRazorPages();

// Pokre?e aplikaciju
app.Run();