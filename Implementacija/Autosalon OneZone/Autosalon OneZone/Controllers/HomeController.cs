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

namespace Autosalon_OneZone.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IHomeService _homeService;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IHomeService homeService,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _logger = logger;
            _userManager = userManager;
            _homeService = homeService;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kontakt(KontaktViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _homeService.AddSupportRequestAsync(user.Id, model.Naslov, model.Sadrzaj);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = _localizer["ContactAccountProblem"].Value;
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

                TempData["SuccessMessage"] = _localizer["ContactMessageSent"].Value;
                return RedirectToAction(nameof(Index));
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
