using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Autosalon_OneZone.Controllers
{
    public class LanguageController : Controller
    {
        private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
        {
            "en-US",
            "bs-Latn-BA"
        };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            if (!SupportedCultures.Contains(culture))
            {
                culture = "en-US";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Content("~/");
            }

            return LocalRedirect(returnUrl);
        }
    }
}
