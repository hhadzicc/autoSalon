using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autosalon_OneZone.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Autosalon_OneZone.Data;

namespace Autosalon_OneZone.Controllers
{
    public class VoziloController : Controller
    {
        private readonly IVoziloService _voziloService;

        public VoziloController(IVoziloService voziloService)
        {
            _voziloService = voziloService;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vozilo = await _voziloService.GetVehicleDetailsAsync(id);

            if (vozilo == null)
            {
                return NotFound();
            }

            return View(vozilo);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, string sortOrder,
            int? godisteOd, int? godisteDo, string gorivo, string boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ViewData["SearchTerm"] = searchTerm;
            }

            if (godisteOd.HasValue)
            {
                ViewData["GodisteOd"] = godisteOd.Value;
            }
            if (godisteDo.HasValue)
            {
                ViewData["GodisteDo"] = godisteDo.Value;
            }

            if (!string.IsNullOrEmpty(gorivo))
            {
                ViewData["Gorivo"] = gorivo;
            }

            if (!string.IsNullOrEmpty(boja))
            {
                ViewData["Boja"] = boja;
            }

            if (kubikazaOd.HasValue)
            {
                ViewData["KubikazaOd"] = kubikazaOd.Value;
            }
            if (kubikazaDo.HasValue)
            {
                ViewData["KubikazaDo"] = kubikazaDo.Value;
            }

            if (kilometrazaOd.HasValue)
            {
                ViewData["KilometrazaOd"] = kilometrazaOd.Value;
            }
            if (kilometrazaDo.HasValue)
            {
                ViewData["KilometrazaDo"] = kilometrazaDo.Value;
            }

            if (cijenaOd.HasValue)
            {
                ViewData["CijenaOd"] = cijenaOd.Value;
            }
            if (cijenaDo.HasValue)
            {
                ViewData["CijenaDo"] = cijenaDo.Value;
            }

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParam"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["YearSortParam"] = sortOrder == "year" ? "year_desc" : "year";

            var vozila = await _voziloService.GetVehiclesAsync(new VehicleSearchCriteria(
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
                cijenaDo));

            return View(vozila);
        }
    }
}
