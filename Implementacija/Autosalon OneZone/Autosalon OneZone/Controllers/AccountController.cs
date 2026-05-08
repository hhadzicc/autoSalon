using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var existingUserByUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(string.Empty, $"Korisničko ime '{model.UserName}' je već zauzeto.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(string.Empty, $"Email '{model.Email}' je već zauzet.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    Ime = model.Ime,
                    Prezime = model.Prezime
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger?.LogInformation("User created a new account with password.");

                    const string kupacRoleName = "Kupac";
                    if (!await _roleManager.RoleExistsAsync(kupacRoleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(kupacRoleName));
                        _logger?.LogInformation($"Rola '{kupacRoleName}' kreirana.");
                    }

                    await _userManager.AddToRoleAsync(user, kupacRoleName);
                    _logger?.LogInformation($"Korisnik '{user.UserName}' dodan u rolu '{kupacRoleName}'.");

                    TempData["SuccessMessage"] = "Registracija uspješna! Sada se možete prijaviti.";

                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName,
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
                    ModelState.AddModelError(string.Empty, "Neispravna email adresa ili šifra.");
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger?.LogInformation("User logged out.");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
