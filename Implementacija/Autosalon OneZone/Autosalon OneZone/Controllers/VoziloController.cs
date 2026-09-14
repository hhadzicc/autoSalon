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

namespace Autosalon_OneZone.Controllers
{
    public class VoziloController : Controller
    {
        private const int PageSize = 6;
        private readonly IVoziloService _voziloService;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public VoziloController(
            IVoziloService voziloService,
            ICartService cartService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _voziloService = voziloService;
            _cartService = cartService;
            _userManager = userManager;
            _localizer = localizer;
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
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
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
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
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
            double? mileageFrom, double? mileageTo,
            decimal? priceFrom, decimal? priceTo)
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
            double? mileageFrom, double? mileageTo,
            decimal? priceFrom, decimal? priceTo,
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

            NormalizeDecimalRange(ref priceFrom, ref priceTo, "PriceFilterError", includeValidationMessages);
            NormalizeDoubleRange(ref mileageFrom, ref mileageTo, "MileageFilterError", includeValidationMessages);
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

        private void NormalizeDoubleRange(
            ref double? from,
            ref double? to,
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
