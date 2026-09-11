using System;
using System.Threading.Tasks;
using Autosalon_OneZone.Models.ViewModels;
using Autosalon_OneZone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountAuthenticationService _authenticationService;
        private readonly IAccountRegistrationService _registrationService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private readonly ILogger<AccountController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(
            IAccountAuthenticationService authenticationService,
            IAccountRegistrationService registrationService,
            IPasswordRecoveryService passwordRecoveryService,
            ILogger<AccountController> logger,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _authenticationService = authenticationService;
            _registrationService = registrationService;
            _passwordRecoveryService = passwordRecoveryService;
            _logger = logger;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
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
                var result = await _registrationService.RegisterAsync(model);
                if (result.Status == AccountRegistrationStatus.UsernameTaken)
                {
                    ModelState.AddModelError(string.Empty, _localizer["UsernameTaken", model.UserName]);
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                if (result.Status == AccountRegistrationStatus.EmailTaken)
                {
                    ModelState.AddModelError(string.Empty, _localizer["EmailTaken", model.Email]);
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                if (result.Status == AccountRegistrationStatus.Succeeded)
                {
                    TempData["SuccessMessage"] = _localizer["RegisterSuccess"].Value;
                    return RedirectToAction("Login", "Account", new { returnUrl });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
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
                var invalidLoginMessage = _localizer["InvalidLogin"];
                var result = await _authenticationService.SignInAsync(
                    model.LoginIdentifier,
                    model.Password,
                    model.RememberMe);

                if (result == AccountSignInStatus.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    return Url.IsLocalUrl(returnUrl)
                        ? Redirect(returnUrl)
                        : RedirectToAction("Index", "Home");
                }

                if (result == AccountSignInStatus.RequiresTwoFactor)
                {
                    ModelState.AddModelError(string.Empty, _localizer["TwoFactorRequired"]);
                }
                else if (result == AccountSignInStatus.LockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty, _localizer["AccountLocked"]);
                }
                else if (result == AccountSignInStatus.NotAllowed)
                {
                    ModelState.AddModelError(string.Empty, _localizer["LoginNotAllowed"]);
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
        [EnableRateLimiting("password-recovery")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resetRequest = await _passwordRecoveryService.CreateResetRequestAsync(model.Email);
            if (resetRequest != null)
            {
                var resetLink = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { userId = resetRequest.UserId, code = resetRequest.EncodedCode },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetLink))
                {
                    await _passwordRecoveryService.SendResetEmailAsync(resetRequest, resetLink);
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

            var result = await _passwordRecoveryService.ResetPasswordAsync(
                model.UserId,
                model.Code,
                model.Password);

            if (result.Status is PasswordResetStatus.Succeeded or PasswordResetStatus.UserNotFound)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            if (result.Status == PasswordResetStatus.InvalidCode)
            {
                ModelState.AddModelError(string.Empty, _localizer["ResetLinkInvalid"]);
                return View(model);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
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
            await _authenticationService.SignOutAsync();
            _logger.LogInformation("User logged out.");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private string TranslateIdentityError(string error)
        {
            if (error.Contains("Invalid token", StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["ResetLinkInvalid"];
            }

            if (error.Contains("Passwords must be at least", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Password must be at least", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one digit", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one lowercase", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one uppercase", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Passwords must have at least one non alphanumeric character", StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["PasswordPolicyError"];
            }

            if (error.Contains("User name", StringComparison.OrdinalIgnoreCase) &&
                error.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["Validation.UsernameAlphanumeric"];
            }

            if (error.Contains("Email", StringComparison.OrdinalIgnoreCase) &&
                error.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
            {
                return _localizer["Validation.EmailValid"];
            }

            return error;
        }
    }
}
