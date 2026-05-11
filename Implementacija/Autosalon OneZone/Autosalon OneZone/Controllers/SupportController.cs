using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Controllers
{
    public class SupportController : Controller
    {
        private readonly ILogger<SupportController> _logger;

        public SupportController(ILogger<SupportController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger?.LogWarning("Received empty support message.");
                TempData["ErrorMessage"] = "Molimo vas unesite tekst poruke pre slanja.";

                return RedirectToAction("Kontakt", "Home");
            }

            _logger?.LogInformation($"Received support message: {message}");
            TempData["SuccessMessage"] = "Vaša poruka je uspešno primljena!";

            return RedirectToAction("Kontakt", "Home");
        }
    }
}
