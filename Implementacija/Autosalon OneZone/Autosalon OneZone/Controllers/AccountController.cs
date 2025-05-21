// Put this file in your Controllers folder, e.g., Autosalon_OneZone/Controllers/AccountController.cs

// Potrebni using directives (proverite da li svi postoje na vrhu fajla)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models; // Namespace gde je ApplicationUser
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Za [AllowAnonymous] i [Authorize]
using Autosalon_OneZone.Models.ViewModels; // Namespace gde su vasi ViewModel-i
using Microsoft.Extensions.Logging; // Za logovanje (opciono ali preporucljivo)


namespace Autosalon_OneZone.Controllers
{
    // Umesto KorisnikController, češće se koristi AccountController za logiku autentifikacije
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Dodato za rad sa rolama
        private readonly ILogger<AccountController> _logger;

        // Konstruktor gde se injektuju potrebni servisi
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager, // Dodato za rad sa rolama
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager; // Dodato za rad sa rolama
            _logger = logger;
        }

        // GET: /Account/Register
        // Prikazuje formu za registraciju
        [HttpGet]
        [AllowAnonymous] // Omogucava neautentifikovanim korisnicima pristup ovoj akciji
        public IActionResult Register(string returnUrl = null) // Dodato rukovanje returnUrl
        {
            ViewData["ReturnUrl"] = returnUrl; // Sačuvaj URL za kasnije preusmeravanje
            // Vraćate View koji sadrži formu. Prosledite novi, prazan ViewModel
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        // Procesira submit forme za registraciju
        [HttpPost]
        [AllowAnonymous] // Omogucava neautentifikovanim korisnicima pristup ovoj akciji
        [ValidateAntiForgeryToken] // Standardna zaštita od CSRF napada
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null) // Dodato rukovanje returnUrl
        {
            returnUrl ??= Url.Content("~/"); // Ako returnUrl nije postavljen (npr. null ili prazan), preusmeri na pocetnu stranicu (~)

            if (ModelState.IsValid) // Proverite validaciju modela (na osnovu Data Annotations na ViewModelu)
            {
                // Kreirajte novu ApplicationUser instancu iz podataka forme
                var user = new ApplicationUser
                {
                    UserName = model.Email, // Obično se Email koristi kao UserName u Identity
                    Email = model.Email,
                    Ime = model.Ime,
                    Prezime = model.Prezime
                    // Popunite ostala dodatna polja iz modela ako postoje
                    // EmailConfirmed = true // Ako ne radite email potvrdu, postavite na true
                };

                // Kreirajte korisnika preko UserManagera
                // UserManager automatski hešira lozinku
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded) // Ako je kreiranje korisnika uspesno
                {
                    _logger?.LogInformation("User created a new account with password."); // Logovanje (opciono)

                    // Proverite da li rola "Kupac" postoji. Ako ne postoji, kreirajte je.
                    const string kupacRoleName = "Kupac";
                    if (!await _roleManager.RoleExistsAsync(kupacRoleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(kupacRoleName));
                        _logger?.LogInformation($"Rola '{kupacRoleName}' kreirana.");
                    }

                    // Dodaj korisnika u rolu "Kupac"
                    await _userManager.AddToRoleAsync(user, kupacRoleName);
                    _logger?.LogInformation($"Korisnik '{user.Email}' dodan u rolu '{kupacRoleName}'.");

                    // Opciono: Prijavite korisnika odmah nakon registracije
                    // Ako zelite da korisnik bude automatski prijavljen, odkomentarisite sledecu liniju
                    // await _signInManager.SignInAsync(user, isPersistent: false); // isPersistent: false znaci session cookie

                    // Preusmerite na neku stranicu nakon uspešne registracije
                    //return LocalRedirect(returnUrl); // Preusmeri na originalni URL ili pocetnu
                    return RedirectToAction("Index", "Home"); // Primer: Preusmeri na Home/Index stranicu
                }

                // Ako registracija nije uspela, dodajte greške u ModelState i vratite View
                // Greske ce biti prikazane u asp-validation-summary tag helperu u Viewu
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Ako podaci iz forme nisu validni (ModelState.IsValid == false)
            // Vratite View sa modelom i greskama ( ModelState ce ih sadrzati)
            ViewData["ReturnUrl"] = returnUrl; // Ponovo postavite returnUrl
            return View(model);
        }

        // ... ostali kod ...
        // Sve ostale akcije ostaju nepromenjene

        // GET: /Account/Login
        // Prikazuje formu za login
        [HttpGet]
        [AllowAnonymous] // Omogucava neautentifikovanim korisnicima pristup ovoj akciji
        public IActionResult Login(string returnUrl = null) // Dodato rukovanje returnUrl
        {
            ViewData["ReturnUrl"] = returnUrl; // Sačuvaj URL za kasnije preusmeravanje
            // Vraćate View koji sadrži formu. Prosledite novi, prazan ViewModel
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        // Procesira submit forme za login
        [HttpPost]
        [AllowAnonymous] // Omogucava neautentifikovanim korisnicima pristup ovoj akciji
        [ValidateAntiForgeryToken] // Bitno za sigurnost
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null) // Dodato rukovanje returnUrl
        {
            returnUrl ??= Url.Content("~/"); // Ako returnUrl nije postavljen, preusmeri na pocetnu stranicu (~)

            if (ModelState.IsValid) // Proveravamo da li su podaci iz forme validni
            {
                // Pokušaj prijavu korisnika
                // Prvi parametar (userName) je obicno Email u Identity podrazumevanoj konfiguraciji
                // isPersistent: model.RememberMe -> Da li ce cookie ostati nakon zatvaranja browsera
                // lockoutOnFailure: false -> Da li ce se nalog zakljucati posle neuspesnih pokusaja (konfigurise se u Identity opcijama)
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, // Koristite Email iz modela (ili UserName ako ste tako konfigurisali)
                    model.Password,
                    model.RememberMe, // Polje za "Zapamti me" iz ViewModel-a
                    lockoutOnFailure: false // Mozete postaviti na true ako zelite lockout
                );

                if (result.Succeeded) // Ako je prijava uspesna
                {
                    _logger?.LogInformation("User logged in."); // Logovanje (opciono)

                    // Preusmeravanje nakon uspesne prijave
                    // Provera da li je returnUrl lokalni radi sigurnosti (sprecava open redirect attack)
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl); // Preusmeri na originalni URL
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home"); // Ako returnUrl nije lokalni ili ne postoji, preusmeri na pocetnu
                    }
                }

                // --- Rukovanje razlicitim stanjima neuspesne prijave ---

                if (result.RequiresTwoFactor)
                {
                    // Ako je potrebna dvofaktorska autentifikacija (ako ste je omogucili)
                    // Redirekt na stranicu za dvofaktorsku autentifikaciju
                    // return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    ModelState.AddModelError(string.Empty, "Potrebna je dvofaktorska autentifikacija.");
                    // Vrati View sa greskom
                }
                else if (result.IsLockedOut)
                {
                    _logger?.LogWarning("User account locked out."); // Logovanje (opciono)
                                                                     // Redirekt na stranicu za zakljucane naloge
                                                                     // return RedirectToPage("./Lockout");
                    ModelState.AddModelError(string.Empty, "Korisnički nalog je privremeno zaključan zbog previše neuspelih pokušaja.");
                    // Vrati View sa greskom
                }
                else if (result.IsNotAllowed)
                {
                    // Ako nalog nije dozvoljen za prijavu (npr. email nije potvrđen, nalog deaktiviran, itd.)
                    ModelState.AddModelError(string.Empty, "Nalog nije dozvoljen za prijavu (npr. email nije potvrđen).");
                    // Vrati View sa greskom
                }
                else
                {
                    // Ako prijava nije uspela iz nekog drugog razloga (npr. pogresna email/lozinka)
                    ModelState.AddModelError(string.Empty, "Neispravna email adresa ili šifra."); // Dodaj gresku za prikaz u validacionom sazetku
                    // Vrati View sa greskom
                }
            }

            // Ako podaci iz forme nisu validni (ModelState.IsValid == false)
            // Vrati View sa unesenim podacima i validacionim greskama
            ViewData["ReturnUrl"] = returnUrl; // Ponovo postavite returnUrl
            return View(model);
        }

        // POST: /Account/Logout
        // Procesira odjavu korisnika
        [HttpPost]
        // [Authorize] // Opciono: Samo prijavljeni korisnici mogu pozvati Logout, ali forma u Layoutu vec pokazuje link samo prijavljenima
        [ValidateAntiForgeryToken] // Bitno za sigurnost
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); // Odjavi trenutnog korisnika (brise cookie)
            _logger?.LogInformation("User logged out."); // Logovanje (opciono)

            // Preusmerite na neku stranicu nakon odjave (npr. pocetna)
            return RedirectToAction("Index", "Home");
        }


        // GET: /Account/AccessDenied
        // Akcija koja se poziva kada korisnik pokuša da pristupi resursu
        // za koji nema dozvolu (npr. zasticena stranica, akcija kontrolera sa [Authorize] atributom)
        // Putanja do ove akcije se konfigurise u Identity opcijama
        [HttpGet]
        [AllowAnonymous] // Dozvolite neautentifikovanim i neautorizovanim korisnicima pristup ovoj stranici
        public IActionResult AccessDenied()
        {
            // Vrati View za "Access Denied" (koji treba da kreirate)
            // View fajl bi bio Views/Account/AccessDenied.cshtml
            return View();
        }
    }
}
