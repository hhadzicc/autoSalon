using System.Diagnostics;
using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.ViewModels;
using Autosalon_OneZone.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Services;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;

namespace Autosalon_OneZone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IHomeService _homeService;
        private readonly ISupportService _supportService;
        private readonly DemoResetSchedule _demoResetSchedule;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IHomeService homeService,
            ISupportService supportService,
            IStringLocalizer<SharedResource>? localizer = null,
            DemoResetSchedule? demoResetSchedule = null)
        {
            _logger = logger;
            _userManager = userManager;
            _homeService = homeService;
            _supportService = supportService;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
            _demoResetSchedule = demoResetSchedule ?? new DemoResetSchedule();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _homeService.GetHomePageAsync());
        }

        [HttpGet]
        public IActionResult Kontakt()
        {
            var viewModel = new KontaktViewModel();

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("support-messages")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kontakt(KontaktViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var ticketId = await _supportService.CreateTicketAsync(
                        user.Id,
                        model.Naslov,
                        model.Sadrzaj,
                        CultureInfo.CurrentUICulture.Name);
                    TempData["SuccessMessage"] = _localizer["ContactMessageSent"].Value;
                    return RedirectToAction("PodrskaDetalji", "Profil", new { id = ticketId });
                }

                TempData["ErrorMessage"] = _localizer["ContactAccountProblem"].Value;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving support message");
                TempData["ErrorMessage"] = _localizer["ContactSendError"].Value;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DemoResetStatus()
        {
            return Json(new
            {
                isResetting = _demoResetSchedule.IsResetting,
                nextResetUtc = _demoResetSchedule.NextResetUtc
            });
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
