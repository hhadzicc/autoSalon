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
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Prvo provjeri da li korisničko ime već postoji
                var existingUserByUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(string.Empty, $"Korisničko ime '{model.UserName}' je već zauzeto.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                // Provjeri da li email već postoji
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(string.Empty, $"Email '{model.Email}' je već zauzet.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                // Kreiraj novog korisnika sa podacima iz forme
                var user = new ApplicationUser
                {
                    UserName = model.UserName, // Koristi korisničko ime iz forme umjesto emaila
                    Email = model.Email,
                    Ime = model.Ime,
                    Prezime = model.Prezime
                };

                // Kreiraj korisnika preko UserManager-a
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger?.LogInformation("User created a new account with password.");

                    // Provjeri da li rola "Kupac" postoji, ako ne postoji, kreiraj je
                    const string kupacRoleName = "Kupac";
                    if (!await _roleManager.RoleExistsAsync(kupacRoleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(kupacRoleName));
                        _logger?.LogInformation($"Rola '{kupacRoleName}' kreirana.");
                    }

                    // Dodaj korisnika u rolu "Kupac"
                    await _userManager.AddToRoleAsync(user, kupacRoleName);
                    _logger?.LogInformation($"Korisnik '{user.UserName}' dodan u rolu '{kupacRoleName}'.");

                    // UKLONJEN DEO koji automatski prijavljuje korisnika
                    // await _signInManager.SignInAsync(user, isPersistent: false);
                    // _logger?.LogInformation($"Korisnik '{user.UserName}' automatski prijavljen nakon registracije.");

                    // Dodaj informativnu poruku da je registracija uspješna
                    TempData["SuccessMessage"] = "Registracija uspješna! Sada se možete prijaviti.";

                    // Preusmjeri na Login stranicu umjesto na početnu
                    return RedirectToAction("Login", "Account");
                }

                // Ako registracija nije uspjela, dodaj greške u ModelState i vrati View
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Ako podaci iz forme nisu validni, vrati View sa greškama
            ViewData["ReturnUrl"] = returnUrl;
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
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Prvo pronađi korisnika po email adresi
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Kad pronađemo korisnika, koristi njegovo korisničko ime za prijavu
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName, // Koristimo korisničko ime pronađenog korisnika
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false
                    );

                    if (result.Succeeded)
                    {
                        _logger?.LogInformation("User logged in.");
                        if (Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }

                    if (result.RequiresTwoFactor)
                    {
                        ModelState.AddModelError(string.Empty, "Potrebna je dvofaktorska autentifikacija.");
                    }
                    else if (result.IsLockedOut)
                    {
                        _logger?.LogWarning("User account locked out.");
                        ModelState.AddModelError(string.Empty, "Korisnički nalog je privremeno zaključan zbog previše neuspjelih pokušaja.");
                    }
                    else if (result.IsNotAllowed)
                    {
                        ModelState.AddModelError(string.Empty, "Nalog nije dozvoljen za prijavu (npr. email nije potvrđen).");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Neispravna email adresa ili šifra.");
                    }
                }
                else
                {
                    // Ako korisnik nije pronađen po email adresi
                    ModelState.AddModelError(string.Empty, "Neispravna email adresa ili šifra.");
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
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
