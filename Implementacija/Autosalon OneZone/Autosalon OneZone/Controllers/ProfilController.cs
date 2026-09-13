using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using Autosalon_OneZone.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Services;

namespace Autosalon_OneZone.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfilController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IProfileActivityService _activityService;
        private readonly IProfileAccountService _accountService;

        public ProfilController(
            UserManager<ApplicationUser> userManager,
            ILogger<ProfilController> logger,
            IProfileActivityService activityService,
            IProfileAccountService accountService,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _userManager = userManager;
            _logger = logger;
            _activityService = activityService;
            _accountService = accountService;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            string role = roles.FirstOrDefault() ?? _localizer["RoleBuyer"].Value;

            return View(await _activityService.GetProfileAsync(user, role));
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            return View(_accountService.GetEditModel(user));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountService.UpdateAsync(user, model);
            if (result.Status is ProfileUpdateStatus.EmailFailure or ProfileUpdateStatus.UsernameFailure)
            {
                var messageKey = result.Status == ProfileUpdateStatus.EmailFailure
                    ? "ProfileUpdateEmailError"
                    : "ProfileUpdateUsernameError";
                ModelState.AddModelError(string.Empty, _localizer[messageKey]);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            if (result.Status is ProfileUpdateStatus.EmailFailure or
                ProfileUpdateStatus.UsernameFailure or
                ProfileUpdateStatus.IdentityFailure)
            {
                return View(model);
            }

            TempData[result.Status == ProfileUpdateStatus.Updated ? "SuccessMessage" : "InfoMessage"] =
                result.Status == ProfileUpdateStatus.Updated
                    ? _localizer["ProfileUpdatedSuccess"].Value
                    : _localizer["ProfileNoChanges"].Value;

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(_localizer["UserNotFound"].Value);
            }

            var result = await _accountService.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return BadRequest(new
                {
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            _logger.LogInformation("User changed their password successfully.");

            return Ok(new { message = _localizer["PasswordChangedSuccess"].Value });
        }

        [HttpGet]
        public async Task<IActionResult> KupljeniArtikli()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            return View(await _activityService.GetPurchasedItemsAsync(user.Id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajRecenziju(int voziloId, int ocjena, string komentar)
        {
            if (ocjena < 1 || ocjena > 5)
            {
                return ReviewError(_localizer["ReviewRatingRange"].Value);
            }

            komentar = komentar?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(komentar))
            {
                return ReviewError(_localizer["CommentRequired"].Value);
            }

            if (komentar.Length > 1000)
            {
                return ReviewError(_localizer["CommentMaxLength"].Value);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var result = await _activityService.SaveReviewAsync(user.Id, voziloId, ocjena, komentar);
            if (result == ReviewSaveResult.NotPurchased)
            {
                return ReviewError(_localizer["ReviewPurchasedOnly"].Value);
            }

            var successMessage = result == ReviewSaveResult.Updated
                ? _localizer["ReviewUpdatedSuccess"].Value
                : _localizer["ReviewAddedSuccess"].Value;
            var reviewId = await _activityService.GetReviewIdAsync(user.Id, voziloId);

            return ReviewSuccess(successMessage, reviewId, ocjena, komentar);
        }

        private bool WantsJsonResponse()
        {
            var requestedWith = Request.Headers["X-Requested-With"].ToString();
            var accept = Request.Headers["Accept"].ToString();

            return string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                   accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult ReviewError(string message, int statusCode = 400)
        {
            if (WantsJsonResponse())
            {
                return new JsonResult(new { success = false, message })
                {
                    StatusCode = statusCode
                };
            }

            TempData["ErrorMessage"] = message;
            return RedirectToAction("KupljeniArtikli");
        }

        private IActionResult ReviewSuccess(string message, int? reviewId, int rating, string comment)
        {
            if (WantsJsonResponse())
            {
                return Json(new
                {
                    success = true,
                    message,
                    review = new
                    {
                        reviewId,
                        rating,
                        comment,
                        date = DateTime.UtcNow.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                    }
                });
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("KupljeniArtikli");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UkloniRecenziju(int recenzijaId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            if (!await _activityService.DeleteReviewAsync(user.Id, recenzijaId))
            {
                TempData["ErrorMessage"] = _localizer["ReviewNotFoundOrNotOwned"].Value;
                return RedirectToAction("KupljeniArtikli");
            }

            return ReviewMutationSuccess(_localizer["ReviewRemovedSuccess"].Value);
        }

        private IActionResult ReviewMutationSuccess(string message)
        {
            if (WantsJsonResponse())
            {
                return Json(new { success = true, message });
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("KupljeniArtikli");
        }
    }
}
