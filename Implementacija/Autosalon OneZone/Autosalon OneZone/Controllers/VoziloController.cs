using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Services;
using Autosalon_OneZone.Validation;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autosalon_OneZone.Models;
using System.Linq;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Autosalon_OneZone.Authorization;
using Autosalon_OneZone.Logging;

namespace Autosalon_OneZone.Controllers
{
    public class VoziloController : Controller
    {
        private const int PageSize = 6;
        private readonly IVoziloService _voziloService;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IVehicleImageStorage _imageStorage;
        private readonly IAuditLogger _audit;

        public VoziloController(
            IVoziloService voziloService,
            ICartService cartService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer,
            IVehicleImageStorage imageStorage,
            IAuditLogger? audit = null)
        {
            _voziloService = voziloService;
            _cartService = cartService;
            _userManager = userManager;
            _localizer = localizer;
            _imageStorage = imageStorage;
            _audit = audit ?? NullAuditLogger.Instance;
        }

        [HttpGet("/vehicle-images/{id:int}/{variant?}")]
        public async Task<IActionResult> Image(
            int id,
            string? variant,
            string? v,
            CancellationToken cancellationToken)
        {
            var vehicle = await _voziloService.GetVehicleDetailsAsync(id, includeUnavailable: true);
            if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.Slika))
            {
                return NotFound();
            }

            var isThumbnail = string.Equals(variant, "thumbnail", StringComparison.OrdinalIgnoreCase);
            var isDetail = string.IsNullOrWhiteSpace(variant) ||
                           string.Equals(variant, "detail", StringComparison.OrdinalIgnoreCase);
            if (!isThumbnail && !isDetail)
            {
                return NotFound();
            }

            var requestedFileName = isThumbnail
                ? VehicleImageNames.ThumbnailFor(vehicle.Slika)
                : vehicle.Slika;
            var image = await _imageStorage.OpenReadAsync(requestedFileName, cancellationToken);
            var isVersionedFile = image != null &&
                                  string.Equals(v, requestedFileName, StringComparison.Ordinal);

            if (image == null && isThumbnail)
            {
                image = await _imageStorage.OpenReadAsync(vehicle.Slika, cancellationToken);
                isVersionedFile = false;
            }

            if (image == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = isVersionedFile
                ? "public,max-age=31536000,immutable"
                : "no-store";
            return File(image.Stream, image.ContentType, enableRangeProcessing: true);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string? returnUrl = null)
        {
            var canManageVehicles = User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Seller);
            var vozilo = await _voziloService.GetVehicleDetailsAsync(id, canManageVehicles);

            if (vozilo == null)
            {
                return NotFound();
            }

            var isAvailableForPurchase = !canManageVehicles ||
                await _voziloService.IsAvailableForPurchaseAsync(id);

            var userId = _userManager.GetUserId(User);
            ViewData["IsInCart"] = isAvailableForPurchase && User.IsInRole(AppRoles.Buyer) && !string.IsNullOrEmpty(userId) &&
                (await _cartService.GetVehicleIdsAsync(userId, new[] { id })).Contains(id);
            ViewData["ReturnUrl"] = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;

            _audit.Success(
                "VehicleDetailsViewed",
                "Vehicle",
                id.ToString(),
                new
                {
                    vozilo.Marka,
                    vozilo.Model,
                    vozilo.Godiste,
                    vozilo.Cijena,
                    AvailableForPurchase = isAvailableForPurchase
                });

            return View(new VehicleDetailsViewModel
            {
                Vehicle = vozilo,
                IsAvailableForPurchase = isAvailableForPurchase,
                CustomerExperiences = isAvailableForPurchase
                    ? await _voziloService.GetCustomerExperienceSummaryAsync()
                    : new CustomerExperienceSummaryViewModel()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, string sortOrder,
            int? godisteOd, int? godisteDo, string gorivo, string[]? boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            int? kilometrazaOd, int? kilometrazaDo,
            int? cijenaOd, int? cijenaDo)
        {
            searchTerm = searchTerm?.Trim();
            var colors = ParseColors(boja);
            PopulateFilterViewData(searchTerm, sortOrder, godisteOd, godisteDo, gorivo, colors,
                kubikazaOd, kubikazaDo, kilometrazaOd, kilometrazaDo, cijenaOd, cijenaDo);

            var criteria = CreateValidatedCriteria(
                searchTerm,
                sortOrder,
                godisteOd,
                godisteDo,
                gorivo,
                colors,
                kubikazaOd,
                kubikazaDo,
                kilometrazaOd,
                kilometrazaDo,
                cijenaOd,
                cijenaDo,
                includeValidationMessages: true);
            var result = await _voziloService.GetVehiclesPageAsync(criteria, 1, PageSize);
            var cards = await CreateCardModelsAsync(result.Vehicles);

            if (!string.IsNullOrWhiteSpace(searchTerm) ||
                !string.IsNullOrWhiteSpace(sortOrder) ||
                godisteOd.HasValue || godisteDo.HasValue ||
                !string.IsNullOrWhiteSpace(gorivo) || colors.Count > 0 ||
                kubikazaOd.HasValue || kubikazaDo.HasValue ||
                kilometrazaOd.HasValue || kilometrazaDo.HasValue ||
                cijenaOd.HasValue || cijenaDo.HasValue)
            {
                _audit.Success(
                    "VehicleSearchPerformed",
                    "VehicleCatalog",
                    details: new
                    {
                        SearchTerm = searchTerm,
                        SortOrder = sortOrder,
                        YearFrom = godisteOd,
                        YearTo = godisteDo,
                        Fuel = gorivo,
                        Colors = colors.Select(color => color.ToString()).ToArray(),
                        DisplacementFrom = kubikazaOd,
                        DisplacementTo = kubikazaDo,
                        MileageFrom = kilometrazaOd,
                        MileageTo = kilometrazaDo,
                        PriceFrom = cijenaOd,
                        PriceTo = cijenaDo,
                        ResultCount = result.TotalCount
                    });
            }

            return View(new VehicleListingViewModel
            {
                Vehicles = cards,
                TotalCount = result.TotalCount,
                CurrentPage = result.Page,
                HasMore = result.HasMore,
                SearchTerm = searchTerm
            });
        }

        [HttpGet]
        public async Task<IActionResult> LoadMore(int page, string searchTerm, string sortOrder,
            int? godisteOd, int? godisteDo, string gorivo, string[]? boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            int? kilometrazaOd, int? kilometrazaDo,
            int? cijenaOd, int? cijenaDo)
        {
            var criteria = CreateValidatedCriteria(
                searchTerm?.Trim(), sortOrder, godisteOd, godisteDo, gorivo, ParseColors(boja),
                kubikazaOd, kubikazaDo, kilometrazaOd, kilometrazaDo,
                cijenaOd, cijenaDo, includeValidationMessages: false);
            var result = await _voziloService.GetVehiclesPageAsync(criteria, page, PageSize);
            var cards = await CreateCardModelsAsync(result.Vehicles);

            Response.Headers["X-Has-More"] = result.HasMore ? "true" : "false";
            return PartialView("_VehicleCards", cards);
        }

        private async Task<IReadOnlyList<VehicleCardViewModel>> CreateCardModelsAsync(
            IReadOnlyList<Vozilo> vehicles)
        {
            var userId = _userManager.GetUserId(User);
            var vehicleIdsInCart = !User.IsInRole(AppRoles.Buyer) || string.IsNullOrEmpty(userId)
                ? new HashSet<int>()
                : await _cartService.GetVehicleIdsAsync(
                    userId,
                    vehicles.Select(vehicle => vehicle.VoziloID));

            return vehicles
                .Select(vehicle => new VehicleCardViewModel
                {
                    Vehicle = vehicle,
                    IsInCart = vehicleIdsInCart.Contains(vehicle.VoziloID)
                })
                .ToList();
        }

        private void PopulateFilterViewData(
            string? searchTerm, string? sortOrder,
            int? yearFrom, int? yearTo, string? fuel, IReadOnlyCollection<TipBoje> colors,
            decimal? displacementFrom, decimal? displacementTo,
            int? mileageFrom, int? mileageTo,
            int? priceFrom, int? priceTo)
        {
            ViewData["SearchTerm"] = searchTerm;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["GodisteOd"] = yearFrom;
            ViewData["GodisteDo"] = yearTo;
            ViewData["Gorivo"] = fuel;
            ViewData["Boje"] = colors;
            ViewData["KubikazaOd"] = displacementFrom;
            ViewData["KubikazaDo"] = displacementTo;
            ViewData["KilometrazaOd"] = mileageFrom;
            ViewData["KilometrazaDo"] = mileageTo;
            ViewData["CijenaOd"] = priceFrom;
            ViewData["CijenaDo"] = priceTo;
        }

        private VehicleSearchCriteria CreateValidatedCriteria(
            string? searchTerm, string? sortOrder,
            int? yearFrom, int? yearTo, string? fuel, IReadOnlyCollection<TipBoje> colors,
            decimal? displacementFrom, decimal? displacementTo,
            int? mileageFrom, int? mileageTo,
            int? priceFrom, int? priceTo,
            bool includeValidationMessages)
        {
            var currentYear = VehicleYearPolicy.MaximumYear;
            if ((yearFrom.HasValue && !VehicleYearPolicy.IsValid(yearFrom.Value)) ||
                (yearTo.HasValue && !VehicleYearPolicy.IsValid(yearTo.Value)))
            {
                if (includeValidationMessages)
                    ViewData["YearFilterError"] = _localizer["FilterYearRangeError", currentYear].Value;
                yearFrom = null;
                yearTo = null;
            }
            else if (yearFrom.HasValue && yearTo.HasValue && yearFrom > yearTo)
            {
                if (includeValidationMessages)
                    ViewData["YearFilterError"] = _localizer["FilterRangeOrderError"].Value;
                yearFrom = null;
                yearTo = null;
            }

            NormalizeIntegerRange(ref priceFrom, ref priceTo, "PriceFilterError", includeValidationMessages);
            NormalizeIntegerRange(ref mileageFrom, ref mileageTo, "MileageFilterError", includeValidationMessages);
            NormalizeDecimalRange(
                ref displacementFrom,
                ref displacementTo,
                "DisplacementFilterError",
                includeValidationMessages);

            return new VehicleSearchCriteria(
                searchTerm, sortOrder, yearFrom, yearTo, fuel, colors,
                displacementFrom, displacementTo, mileageFrom, mileageTo,
                priceFrom, priceTo);
        }

        private static IReadOnlyList<TipBoje> ParseColors(IEnumerable<string>? values)
        {
            if (values == null)
            {
                return Array.Empty<TipBoje>();
            }

            return values
                .Select(value => Enum.TryParse<TipBoje>(value, ignoreCase: true, out var color) &&
                                 Enum.IsDefined(typeof(TipBoje), color)
                    ? (TipBoje?)color
                    : null)
                .Where(color => color.HasValue)
                .Select(color => color!.Value)
                .Distinct()
                .ToArray();
        }

        private void NormalizeDecimalRange(
            ref decimal? from,
            ref decimal? to,
            string errorKey,
            bool includeValidationMessages)
        {
            if ((from.HasValue && from < 0) || (to.HasValue && to < 0))
            {
                if (includeValidationMessages)
                    ViewData[errorKey] = _localizer["FilterNonNegativeError"].Value;
                from = null;
                to = null;
            }
            else if (from.HasValue && to.HasValue && from > to)
            {
                if (includeValidationMessages)
                    ViewData[errorKey] = _localizer["FilterRangeOrderError"].Value;
                from = null;
                to = null;
            }
        }

        private void NormalizeIntegerRange(
            ref int? from,
            ref int? to,
            string errorKey,
            bool includeValidationMessages)
        {
            if ((from.HasValue && from < 0) || (to.HasValue && to < 0))
            {
                if (includeValidationMessages)
                    ViewData[errorKey] = _localizer["FilterNonNegativeError"].Value;
                from = null;
                to = null;
            }
            else if (from.HasValue && to.HasValue && from > to)
            {
                if (includeValidationMessages)
                    ViewData[errorKey] = _localizer["FilterRangeOrderError"].Value;
                from = null;
                to = null;
            }
        }
    }
}
