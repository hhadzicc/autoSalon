using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Autosalon_OneZone.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Authorization;

namespace Autosalon_OneZone.Controllers
{
    [Authorize]
    public class KorpaController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<KorpaController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICartService _cartService;
        private readonly ICheckoutService _checkoutService;

        public KorpaController(
            UserManager<ApplicationUser> userManager,
            ILogger<KorpaController> logger,
            ICartService cartService,
            ICheckoutService checkoutService,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _userManager = userManager;
            _logger = logger;
            _cartService = cartService;
            _checkoutService = checkoutService;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        private string GetCartReturnUrl()
        {
            var referer = Request.Headers["Referer"].ToString();
            return string.IsNullOrWhiteSpace(referer) ? "/Vozilo" : referer;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajUKorpu(int id)
        {
            var isAjaxRequest = string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Korisnik nije prijavljen iako je zaštićeno sa [Authorize]");
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = Url.Action("Index", "Vozilo") });
                }

                var result = await _cartService.AddVehicleAsync(user.Id, id);
                var message = result switch
                {
                    CartAddResult.VehicleNotFound => _localizer["VehicleNotFound"].Value,
                    CartAddResult.VehicleAlreadyPurchased => _localizer["VehicleAlreadyPurchased"].Value,
                    CartAddResult.AlreadyAdded => _localizer["CartVehicleAlreadyAdded"].Value,
                    _ => _localizer["CartAddVehicleSuccess"].Value
                };

                if (isAjaxRequest)
                {
                    var success = result is CartAddResult.Added or CartAddResult.AlreadyAdded;
                    var statusCode = result switch
                    {
                        CartAddResult.VehicleNotFound => StatusCodes.Status404NotFound,
                        CartAddResult.VehicleAlreadyPurchased => StatusCodes.Status409Conflict,
                        _ => StatusCodes.Status200OK
                    };

                    return StatusCode(statusCode, new
                    {
                        success,
                        alreadyAdded = result == CartAddResult.AlreadyAdded,
                        message,
                        cartCount = await _cartService.GetItemCountAsync(user.Id)
                    });
                }

                switch (result)
                {
                    case CartAddResult.VehicleNotFound:
                        TempData["ErrorMessage"] = message;
                        break;
                    case CartAddResult.VehicleAlreadyPurchased:
                        TempData["ErrorMessage"] = message;
                        break;
                    case CartAddResult.AlreadyAdded:
                        TempData["SuccessMessage"] = message;
                        break;
                    default:
                        TempData["SuccessMessage"] = message;
                        break;
                }

                return Redirect(GetCartReturnUrl());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri dodavanju vozila u korpu: {ex.Message}");
                if (isAjaxRequest)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        success = false,
                        message = _localizer["CartAddError"].Value
                    });
                }

                TempData["ErrorMessage"] = _localizer["CartAddError"].Value;
                return Redirect(GetCartReturnUrl());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                return View(await _cartService.GetCartAsync(user!.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri prikazivanju korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartDisplayError"].Value;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IzvrsiPlacanje(int VoziloID, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            try
            {
                var paymentForm = PaymentFormValidator.Validate(
                    ImeVlasnika,
                    BrojKartice,
                    DatumIsteka,
                    Cvv,
                    _localizer);

                if (!paymentForm.IsValid)
                {
                    return Json(new { success = false, errors = paymentForm.Errors });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = _localizer["UserNotFound"].Value });
                }

                var checkout = await _checkoutService.PurchaseVehicleAsync(
                    user,
                    VoziloID,
                    ImeVlasnika,
                    DatumIsteka,
                    paymentForm);

                if (checkout.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = _localizer["PaymentOrderRecorded"].Value,
                        redirectUrl = Url.Action("Uspjeh", "Korpa", new { id = checkout.OrderId })
                    });
                }

                if (checkout.Status == CheckoutStatus.PaymentFailed)
                {
                    _logger.LogWarning(
                        "Neuspjesno placanje za vozilo ID: {VoziloID}, iznos: {Iznos}, korisnik: {KorisnikId}, razlog: {Razlog}",
                        VoziloID,
                        checkout.Amount,
                        user.Id,
                        checkout.PaymentMessage);
                    return Json(new { success = false, message = checkout.PaymentMessage });
                }

                var message = checkout.Status switch
                {
                    CheckoutStatus.VehicleNotFound => _localizer["VehicleNotFound"].Value,
                    CheckoutStatus.VehicleAlreadyPurchased => _localizer["VehicleAlreadyPurchased"].Value,
                    CheckoutStatus.CheckoutBusy => _localizer["CheckoutBusy"].Value,
                    _ => _localizer["VehiclePriceInvalid"].Value
                };
                return Json(new { success = false, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska prilikom izvrsavanja placanja: {Message}", ex.Message);
                return Json(new { success = false, message = _localizer["PaymentProcessingError"].Value });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> IzvrsiPlacanjeSvih(string OdabranaVozilaJSON, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            try
            {
                _logger.LogInformation("Početak obrade grupnog plaćanja");
                _logger.LogInformation("Primljeni JSON: {OdabranaVozilaJSON}", OdabranaVozilaJSON);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                List<OdabranoVoziloViewModel>? odabranaVozila = null;
                if (!string.IsNullOrWhiteSpace(OdabranaVozilaJSON))
                {
                    try
                    {
                        odabranaVozila = JsonSerializer.Deserialize<List<OdabranoVoziloViewModel>>(OdabranaVozilaJSON, options);
                    }
                    catch (JsonException)
                    {
                        return Json(new { success = false, message = _localizer["NoVehiclesSelectedForPurchase"].Value });
                    }
                }

                if (odabranaVozila == null || !odabranaVozila.Any())
                {
                    return Json(new { success = false, message = _localizer["NoVehiclesSelectedForPurchase"].Value });
                }

                _logger.LogInformation("Broj odabranih vozila: {Count}", odabranaVozila.Count);

                var paymentForm = PaymentFormValidator.Validate(
                    ImeVlasnika,
                    BrojKartice,
                    DatumIsteka,
                    Cvv,
                    _localizer,
                    "checkout");

                if (!paymentForm.IsValid)
                {
                    return Json(new { success = false, errors = paymentForm.Errors });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = _localizer["UserNotFound"].Value });
                }

                var odabraniVozilaIds = odabranaVozila.Select(v => v.id).ToHashSet();
                _logger.LogInformation("Odabrana vozila IDs: {IDs}", string.Join(", ", odabraniVozilaIds));

                var checkout = await _checkoutService.PurchaseCartItemsAsync(
                    user,
                    odabraniVozilaIds,
                    ImeVlasnika,
                    DatumIsteka,
                    paymentForm);

                if (checkout.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = string.Format(_localizer["MultiVehiclePurchaseSuccess"].Value, checkout.PurchasedCount),
                        redirectUrl = Url.Action("Uspjeh", "Korpa", new { id = checkout.OrderId })
                    });
                }

                if (checkout.Status == CheckoutStatus.PaymentFailed)
                {
                    _logger.LogWarning("Neuspješno grupno plaćanje, razlog: {Message}", checkout.PaymentMessage);
                    return Json(new { success = false, message = checkout.PaymentMessage });
                }

                var message = checkout.Status switch
                {
                    CheckoutStatus.CartNotFound => _localizer["CartNotFound"].Value,
                    CheckoutStatus.VehicleAlreadyPurchased => _localizer["VehicleAlreadyPurchased"].Value,
                    CheckoutStatus.CheckoutBusy => _localizer["CheckoutBusy"].Value,
                    _ => _localizer["SelectedVehicleMissingFromCart"].Value
                };
                return Json(new { success = false, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stvarna greška prilikom obrade grupnog plaćanja: {Message}", ex.Message);
                return Json(new { success = false, message = _localizer["PaymentProcessingError"].Value });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Uspjeh(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var narudzba = await _checkoutService.GetOrderAsync(
                id,
                user.Id,
                User.IsInRole(AppRoles.Administrator));

            if (narudzba == null)
            {
                return Forbid();
            }

            return View(narudzba);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UkloniIzKorpe(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _cartService.RemoveVehicleAsync(user!.Id, id);
                if (result == CartRemoveResult.Removed)
                {
                    TempData["SuccessMessage"] = _localizer["CartRemoveSuccess"].Value;
                }
                else if (result == CartRemoveResult.VehicleNotFound)
                {
                    TempData["InfoMessage"] = _localizer["CartVehicleNotFoundInCart"].Value;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri uklanjanju vozila iz korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartRemoveError"].Value;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcistiKorpu()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                await _cartService.ClearAsync(user!.Id);

                TempData["SuccessMessage"] = _localizer["CartClearedSuccess"].Value;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri čišćenju korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartClearError"].Value;
                return RedirectToAction("Index");
            }
        }
    }
}
