using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Autosalon_OneZone.Models.ViewModels;
using Autosalon_OneZone.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;

namespace Autosalon_OneZone.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
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
                var loginIdentifier = model.LoginIdentifier.Trim();
                var invalidLoginMessage = "Neispravan e-mail, korisničko ime ili lozinka.";

                var user = await _userManager.FindByEmailAsync(loginIdentifier);
                user ??= await _userManager.FindByNameAsync(loginIdentifier);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName!,
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
                        ModelState.AddModelError(string.Empty, invalidLoginMessage);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, invalidLoginMessage);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var resetLink = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { userId = user.Id, code = encodedToken },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetLink))
                {
                    try
                    {
                        var displayName = $"{user.Ime} {user.Prezime}".Trim();
                        await _emailSender.SendPasswordResetEmailAsync(
                            user.Email!,
                            string.IsNullOrWhiteSpace(displayName) ? user.UserName ?? user.Email! : displayName,
                            resetLink,
                            DateTime.UtcNow.AddMinutes(30));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email for {Email}.", model.Email);
                    }
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId = null, string code = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Code = code
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Link za resetovanje lozinke nije ispravan ili je istekao.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Description));
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
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

        private static string TranslateIdentityError(string error)
        {
            if (error.Contains("Invalid token", StringComparison.OrdinalIgnoreCase))
            {
                return "Link za resetovanje lozinke nije ispravan ili je istekao.";
            }

            if (error.Contains("Passwords must be at least", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Password must be at least", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one digit", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one lowercase", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one uppercase", StringComparison.OrdinalIgnoreCase))
            {
                return "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.";
            }

            return error;
        }
    }
}
