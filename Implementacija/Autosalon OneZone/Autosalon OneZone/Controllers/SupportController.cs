using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace Autosalon_OneZone.Controllers
{
    public class SupportController : Controller
    {
        private readonly ILogger<SupportController> _logger;

        public SupportController(ILogger<SupportController> logger = null)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string message)
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
