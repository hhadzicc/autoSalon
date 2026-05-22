using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Controllers
{
    public class SupportController : Controller
    {
        private readonly ILogger<SupportController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public SupportController(ILogger<SupportController> logger, IStringLocalizer<SharedResource>? localizer = null)
        {
            _logger = logger;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger?.LogWarning("Received empty support message.");
                TempData["ErrorMessage"] = _localizer["SupportMessageRequired"].Value;

                return RedirectToAction("Kontakt", "Home");
            }

            _logger?.LogInformation("Received support message: {Message}", message);
            TempData["SuccessMessage"] = _localizer["SupportMessageReceived"].Value;

            return RedirectToAction("Kontakt", "Home");
        }
    }
}
