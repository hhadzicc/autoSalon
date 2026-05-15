using System.Diagnostics;
using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.ViewModels;
using Autosalon_OneZone.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Autosalon_OneZone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var featuredVehicle = await _context.Vozila
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.Marka == "Porsche" &&
                    v.Model == "Panamera 4 E-Hybrid");

            var curatedVehicles = await _context.Vozila
                .AsNoTracking()
                .Where(v =>
                    (v.Marka == "Audi" && v.Model == "e-tron GT quattro") ||
                    (v.Marka == "BMW" && v.Model == "M4 Competition") ||
                    (v.Marka == "Mercedes-Benz" && v.Model == "GLC 300"))
                .ToListAsync();

            var curatedOrder = new Dictionary<string, int>
            {
                ["Audi|e-tron GT quattro"] = 0,
                ["BMW|M4 Competition"] = 1,
                ["Mercedes-Benz|GLC 300"] = 2
            };

            var featuredVehicles = curatedVehicles
                .OrderBy(v => curatedOrder.GetValueOrDefault($"{v.Marka}|{v.Model}", int.MaxValue))
                .Take(3)
                .ToList();

            if (featuredVehicles.Count < 3)
            {
                var excludedVehicleIds = featuredVehicles
                    .Select(v => v.VoziloID)
                    .ToList();

                if (featuredVehicle != null)
                {
                    excludedVehicleIds.Add(featuredVehicle.VoziloID);
                }

                var fallbackVehicles = await _context.Vozila
                    .AsNoTracking()
                    .Where(v => !excludedVehicleIds.Contains(v.VoziloID))
                    .OrderByDescending(v => v.Godiste ?? 0)
                    .ThenByDescending(v => v.Cijena ?? 0)
                    .Take(3 - featuredVehicles.Count)
                    .ToListAsync();

                featuredVehicles.AddRange(fallbackVehicles);
            }

            featuredVehicles = featuredVehicles
                .OrderByDescending(v => v.Godiste ?? 0)
                .ThenByDescending(v => v.Cijena ?? 0)
                .Take(3)
                .ToList();

            var viewModel = new HomeIndexViewModel
            {
                FeaturedVehicle = featuredVehicle,
                FeaturedVehicles = featuredVehicles
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Kontakt()
        {
            var viewModel = new KontaktViewModel();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kontakt(KontaktViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var podrska = new Podrska
                {
                    Naslov = model.Naslov,
                    Sadrzaj = model.Sadrzaj,
                    DatumUpita = DateTime.Now,
                    Status = StatusUpita.Poslat
                };

                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        podrska.KorisnikId = user.Id;
                        podrska.Korisnik = user;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Došlo je do problema sa vašim korisničkim nalogom.";
                        return View(model);
                    }
                }
                else
                {
                    string subject = Uri.EscapeDataString(model.Naslov);
                    string body = Uri.EscapeDataString(model.Sadrzaj);
                    string mailtoUrl = $"mailto:autosalon@autosalon.com?subject={subject}&body={body}";
                    return Redirect(mailtoUrl);
                }

                _context.PodrskaUpiti.Add(podrska);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vaša poruka je uspješno poslana!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving support message");
                TempData["ErrorMessage"] = "Došlo je do greške prilikom slanja poruke. Molimo pokušajte ponovo.";
                return View(model);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
