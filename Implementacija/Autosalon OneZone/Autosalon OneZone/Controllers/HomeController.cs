using System.Diagnostics;
using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.ViewModels;
using Autosalon_OneZone.Data;
using Microsoft.AspNetCore.Identity;
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

        public IActionResult Index()
        {
            return View();
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
