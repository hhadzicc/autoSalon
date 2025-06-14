using Autosalon_OneZone.Models;
using Autosalon_OneZone.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Autosalon_OneZone.Data;
using Stripe;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dodaj Data Protection s perzistencijom u folderu Keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")))
    .SetApplicationName("AutosalonOneZone");

// Dodaj podršku za X-Forwarded-Proto header
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireProdavacRole", policy => policy.RequireRole("Prodavac"));
    options.AddPolicy("RequireKupacRole", policy => policy.RequireRole("Kupac"));
});

builder.Services.AddScoped<IVoziloService, VoziloService>();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        string adminRoleName = "Administrator";
        string adminEmail = configuration["AdminUserSecrets:Email"];
        string adminPassword = configuration["AdminUserSecrets:Password"];
        string adminFirstName = configuration["AdminUserSecrets:FirstName"] ?? "";
        string adminLastName = configuration["AdminUserSecrets:LastName"] ?? "";

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("UPOZORENJE: Admin korisnički Email ili Lozinka nisu pronađeni u konfiguraciji ('AdminUserSecrets' sekcija u appsettings.json). Admin korisnik možda neće biti kreiran.");
            Console.WriteLine("UPOZORENJE: Admin korisnički Email ili Lozinka nisu pronađeni u konfiguraciji ('AdminUserSecrets' sekcija u appsettings.json). Admin korisnik možda neće biti kreiran.");
        }
        else
        {
            if (!await roleManager.RoleExistsAsync(adminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRoleName));
                logger.LogInformation($"Rola '{adminRoleName}' kreirana.");
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Ime = adminFirstName,
                    Prezime = adminLastName
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                    logger.LogInformation($"Admin korisnik '{adminEmail}' kreiran i dodeljena mu je rola '{adminRoleName}'.");
                }
                else
                {
                    Console.WriteLine($"Greška pri kreiranju admin korisnika '{adminEmail}': {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                    logger.LogError($"Failed to create admin user '{adminEmail}': {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
                {
                    await userManager.AddToRoleAsync(adminUser, adminRoleName);
                    logger.LogInformation($"Admin korisnik '{adminEmail}' već postoji, dodeljena mu je rola '{adminRoleName}'.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Došlo je do greške prilikom inicijalizacije Identity podataka (rola i admin korisnika).");
    }

    // Kreiraj folder Keys ako ne postoji
    try
    {
        var keysPath = Path.Combine(app.Environment.ContentRootPath, "Keys");
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
        }
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Došlo je do greške prilikom kreiranja direktorija za Data Protection ključeve.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Primjeni forwarded headers prije HTTPS redirekcije
app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
