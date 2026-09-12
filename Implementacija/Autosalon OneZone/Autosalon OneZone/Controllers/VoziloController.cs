using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autosalon_OneZone.Models;
using System.Linq;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

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
        public async Task<IActionResult> Details(int id)
        {
            var vozilo = await _voziloService.GetVehicleDetailsAsync(id);

            if (vozilo == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            ViewData["IsInCart"] = !string.IsNullOrEmpty(userId) &&
                (await _cartService.GetVehicleIdsAsync(userId, new[] { id })).Contains(id);

            return View(new VehicleDetailsViewModel
            {
                Vehicle = vozilo,
                CustomerExperiences = await _voziloService.GetCustomerExperienceSummaryAsync()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, string sortOrder,
            int? godisteOd, int? godisteDo, string gorivo, string boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
        {
            searchTerm = searchTerm?.Trim();
            PopulateFilterViewData(searchTerm, sortOrder, godisteOd, godisteDo, gorivo, boja,
                kubikazaOd, kubikazaDo, kilometrazaOd, kilometrazaDo, cijenaOd, cijenaDo);

            var criteria = CreateValidatedCriteria(
                searchTerm,
                sortOrder,
                godisteOd,
                godisteDo,
                gorivo,
                boja,
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
            int? godisteOd, int? godisteDo, string gorivo, string boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
        {
            var criteria = CreateValidatedCriteria(
                searchTerm?.Trim(), sortOrder, godisteOd, godisteDo, gorivo, boja,
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
            var vehicleIdsInCart = string.IsNullOrEmpty(userId)
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
            int? yearFrom, int? yearTo, string? fuel, string? color,
            decimal? displacementFrom, decimal? displacementTo,
            double? mileageFrom, double? mileageTo,
            decimal? priceFrom, decimal? priceTo)
        {
            ViewData["SearchTerm"] = searchTerm;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["GodisteOd"] = yearFrom;
            ViewData["GodisteDo"] = yearTo;
            ViewData["Gorivo"] = fuel;
            ViewData["Boja"] = color;
            ViewData["KubikazaOd"] = displacementFrom;
            ViewData["KubikazaDo"] = displacementTo;
            ViewData["KilometrazaOd"] = mileageFrom;
            ViewData["KilometrazaDo"] = mileageTo;
            ViewData["CijenaOd"] = priceFrom;
            ViewData["CijenaDo"] = priceTo;
        }

        private VehicleSearchCriteria CreateValidatedCriteria(
            string? searchTerm, string? sortOrder,
            int? yearFrom, int? yearTo, string? fuel, string? color,
            decimal? displacementFrom, decimal? displacementTo,
            double? mileageFrom, double? mileageTo,
            decimal? priceFrom, decimal? priceTo,
            bool includeValidationMessages)
        {
            var currentYear = DateTime.Now.Year;
            if ((yearFrom.HasValue && (yearFrom < 1900 || yearFrom > currentYear)) ||
                (yearTo.HasValue && (yearTo < 1900 || yearTo > currentYear)))
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
                searchTerm, sortOrder, yearFrom, yearTo, fuel, color,
                displacementFrom, displacementTo, mileageFrom, mileageTo,
                priceFrom, priceTo);
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
