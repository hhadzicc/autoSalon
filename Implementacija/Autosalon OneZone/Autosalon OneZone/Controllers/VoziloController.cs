// Put this file in your Controllers folder, e.g., Autosalon_OneZone/Controllers/VoziloController.cs
// OVO JE ZA PRIKAZ DETALJA VOZILA
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Services; // Potrebno za IVoziloService
using System.Threading.Tasks; // Potrebno za async/await
using System.Collections.Generic; // Potrebno za IEnumerable
using Autosalon_OneZone.Models; // Potrebno za Vozilo model i ViewModele (ako se koriste za Create/Edit)
using Microsoft.AspNetCore.Authorization; // Potrebno za [Authorize]
using System.Linq; // Potrebno za LINQ metode
using Microsoft.EntityFrameworkCore;
using Autosalon_OneZone.Data; // Potrebno za FirstOrDefaultAsync i ostale EF Core metode

namespace Autosalon_OneZone.Controllers
{
    public class VoziloController : Controller
    {
        private readonly IVoziloService _voziloService;
        private readonly ApplicationDbContext _context;

        // Injektovanje IVoziloService i ApplicationDbContext kroz konstruktor
        public VoziloController(IVoziloService voziloService, ApplicationDbContext context)
        {
            _voziloService = voziloService;
            _context = context;
        }

        // GET: /Vozilo/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Dohvati vozilo iz baze podataka zajedno sa recenzijama i korisnicima
            var vozilo = await _context.Vozila
                .Include(v => v.Recenzije)
                    .ThenInclude(r => r.Korisnik)
                .FirstOrDefaultAsync(m => m.VoziloID == id);

            if (vozilo == null)
            {
                return NotFound();
            }

            // Order reviews by date (newest first)
            if (vozilo.Recenzije != null)
            {
                vozilo.Recenzije = vozilo.Recenzije.OrderByDescending(r => r.DatumRecenzije).ToList();
            }

            return View(vozilo);
        }


        // GET: /Vozilo/Index
        // PUT THIS NEW ACTION IN VoziloController.cs
        public async Task<IActionResult> Index(string searchTerm, string sortOrder,
            int? godisteOd, int? godisteDo, string gorivo, string boja,
            decimal? kubikazaOd, decimal? kubikazaDo,
            double? kilometrazaOd, double? kilometrazaDo,
            decimal? cijenaOd, decimal? cijenaDo)
        {
            // Get all vehicles
            var vozila = await _voziloService.GetAllVozilaAsync();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchWords = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                vozila = vozila.Where(v =>
                    searchWords.Any(word =>
                        (v.Marka?.ToLower().Contains(word) ?? false) ||
                        (v.Model?.ToLower().Contains(word) ?? false)
                    )
                ).ToList();
                ViewData["SearchTerm"] = searchTerm;
            }

            // Apply year range filter
            if (godisteOd.HasValue)
            {
                vozila = vozila.Where(v => v.Godiste >= godisteOd.Value).ToList();
                ViewData["GodisteOd"] = godisteOd.Value;
            }
            if (godisteDo.HasValue)
            {
                vozila = vozila.Where(v => v.Godiste <= godisteDo.Value).ToList();
                ViewData["GodisteDo"] = godisteDo.Value;
            }

            // Apply fuel type filter
            if (!string.IsNullOrEmpty(gorivo))
            {
                vozila = vozila.Where(v => v.Gorivo.ToString() == gorivo).ToList();
                ViewData["Gorivo"] = gorivo;
            }

            // Apply color filter
            if (!string.IsNullOrEmpty(boja))
            {
                vozila = vozila.Where(v => v.Boja != null && v.Boja.ToLower().Contains(boja.ToLower())).ToList();
                ViewData["Boja"] = boja;
            }

            // Apply engine size range filter
            if (kubikazaOd.HasValue)
            {
                vozila = vozila.Where(v => v.Kubikaza >= kubikazaOd.Value).ToList();
                ViewData["KubikazaOd"] = kubikazaOd.Value;
            }
            if (kubikazaDo.HasValue)
            {
                vozila = vozila.Where(v => v.Kubikaza <= kubikazaDo.Value).ToList();
                ViewData["KubikazaDo"] = kubikazaDo.Value;
            }

            // Apply mileage range filter
            if (kilometrazaOd.HasValue)
            {
                vozila = vozila.Where(v => v.Kilometraza >= kilometrazaOd.Value).ToList();
                ViewData["KilometrazaOd"] = kilometrazaOd.Value;
            }
            if (kilometrazaDo.HasValue)
            {
                vozila = vozila.Where(v => v.Kilometraza <= kilometrazaDo.Value).ToList();
                ViewData["KilometrazaDo"] = kilometrazaDo.Value;
            }

            // Apply price range filter
            if (cijenaOd.HasValue)
            {
                vozila = vozila.Where(v => v.Cijena >= cijenaOd.Value).ToList();
                ViewData["CijenaOd"] = cijenaOd.Value;
            }
            if (cijenaDo.HasValue)
            {
                vozila = vozila.Where(v => v.Cijena <= cijenaDo.Value).ToList();
                ViewData["CijenaDo"] = cijenaDo.Value;
            }

            // Apply sorting
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParam"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["YearSortParam"] = sortOrder == "year" ? "year_desc" : "year";

            switch (sortOrder)
            {
                case "name_desc":
                    vozila = vozila.OrderByDescending(v => v.Marka).ThenByDescending(v => v.Model).ToList();
                    break;
                case "price":
                    vozila = vozila.OrderBy(v => v.Cijena).ToList();
                    break;
                case "price_desc":
                    vozila = vozila.OrderByDescending(v => v.Cijena).ToList();
                    break;
                case "year":
                    vozila = vozila.OrderBy(v => v.Godiste).ToList();
                    break;
                case "year_desc":
                    vozila = vozila.OrderByDescending(v => v.Godiste).ToList();
                    break;
                default: // Default sort by name ascending
                    vozila = vozila.OrderBy(v => v.Marka).ThenBy(v => v.Model).ToList();
                    break;
            }

            return View(vozila);
        }


        // --- Akcije za ADMINISTRACIJU VOZILA (samo za rolu Administrator) ---

        // GET: /Vozilo/Create
        // Akcija za prikaz forme za dodavanje novog vozila
        [Authorize(Policy = "RequireAdminRole")] // Samo Admini mogu videti ovu formu
        [HttpGet]
        public IActionResult Create()
        {
            // Vraćamo View koji sadrži formu za unos podataka novog vozila (Views/Vozilo/Create.cshtml)
            return View();
        }

        // POST: /Vozilo/Create
        // Akcija za procesiranje forme i dodavanje novog vozila u bazu
        [Authorize(Policy = "RequireAdminRole")] // Samo Admini mogu izvršiti ovu akciju
        [HttpPost]
        [ValidateAntiForgeryToken] // Zaštita od CSRF napada
        public async Task<IActionResult> Create(Vozilo novoVozilo) // Model Binding: ASP.NET Core mapira podatke iz forme na Vozilo objekat
        {
            // Proveravamo server-side validaciju (na osnovu Data Annotations u Vozilo modelu)
            if (ModelState.IsValid)
            {
                // Pozivamo servis da doda novo vozilo u bazu
                var addedVozilo = await _voziloService.AddVoziloAsync(novoVozilo);

                // Nakon uspešnog dodavanja, preusmeravamo korisnika nazad na listu vozila
                // nameof(Index) se koristi umesto stringa "Index" radi sigurnosti (ako preimenujete akciju)
                return RedirectToAction(nameof(Index));
            }

            // Ako validacija ne prođe, vraćamo istu formu sa popunjenim podacima i greškama validacije
            return View(novoVozilo);
        }


      

    }
}
