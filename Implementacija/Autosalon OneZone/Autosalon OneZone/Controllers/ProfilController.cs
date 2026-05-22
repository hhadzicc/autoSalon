using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using Autosalon_OneZone.Data;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Localization;

namespace Autosalon_OneZone.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ProfilController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ProfilController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ProfilController> logger,
            ApplicationDbContext context,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            string role = roles.FirstOrDefault() ?? _localizer["RoleBuyer"].Value;

            var userReviews = await _context.Recenzije
                .Include(r => r.Vozilo)
                .Where(r => r.KorisnikId == user.Id)
                .Select(r => new ProfileViewModel.ReviewViewModel
                {
                    VoziloNaziv = $"{r.Vozilo.Marka} {r.Vozilo.Model}",
                    Ocena = r.Ocjena,
                    Tekst = r.Komentar
                })
                .ToListAsync();

            var model = new ProfileViewModel
            {
                ImePrezime = $"{user.Ime} {user.Prezime}",
                Email = user.Email,
                UserName = user.UserName,
                Role = role,
                Recenzije = userReviews
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var model = new EditProfileViewModel
            {
                Ime = user.Ime,
                Prezime = user.Prezime,
                Email = user.Email,
                UserName = user.UserName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool profileChanged = false;

            if (user.Ime != model.Ime)
            {
                user.Ime = model.Ime;
                profileChanged = true;
            }

            if (user.Prezime != model.Prezime)
            {
                user.Prezime = model.Prezime;
                profileChanged = true;
            }

            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, _localizer["ProfileUpdateEmailError"]);
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                profileChanged = true;
            }

            if (model.UserName != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, _localizer["ProfileUpdateUsernameError"]);
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                profileChanged = true;
            }

            if (profileChanged)
            {
                var updateProfileResult = await _userManager.UpdateAsync(user);
                if (!updateProfileResult.Succeeded)
                {
                    foreach (var error in updateProfileResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] = _localizer["ProfileUpdatedSuccess"].Value;
            }
            else
            {
                TempData["InfoMessage"] = _localizer["ProfileNoChanges"].Value;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(_localizer["UserNotFound"].Value);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(new
                {
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");

            return Ok(new { message = _localizer["PasswordChangedSuccess"].Value });
        }

        [HttpGet]
        public async Task<IActionResult> KupljeniArtikli()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var narudzbe = await _context.Narudzbe
                .Where(n => n.KorisnikId == user.Id)
                .Include(n => n.StavkeKorpe)
                    .ThenInclude(s => s.Vozilo)
                .OrderByDescending(n => n.DatumNarudzbe)
                .ToListAsync();

            var recenzije = await _context.Recenzije
                .Where(r => r.KorisnikId == user.Id)
                .ToListAsync();

            var model = new PurchasedItemsViewModel();

            var uniqueVehicles = new Dictionary<int, PurchasedItemsViewModel.PurchasedItemViewModel>();

            foreach (var narudzba in narudzbe)
            {
                foreach (var stavka in narudzba.StavkeKorpe)
                {
                    if (!uniqueVehicles.ContainsKey(stavka.VoziloID) ||
                        narudzba.DatumNarudzbe > uniqueVehicles[stavka.VoziloID].DatumKupovine)
                    {
                        var recenzija = recenzije.FirstOrDefault(r => r.VoziloID == stavka.VoziloID);

                        var purchasedItem = new PurchasedItemsViewModel.PurchasedItemViewModel
                        {
                            VoziloID = stavka.VoziloID,
                            Naziv = $"{stavka.Vozilo.Marka} {stavka.Vozilo.Model}",
                            Slika = !string.IsNullOrEmpty(stavka.Vozilo.Slika) ? $"/images/vozila/{stavka.Vozilo.Slika}" : "/img/no-image.png",
                            Cijena = stavka.CijenaStavke,
                            DatumKupovine = narudzba.DatumNarudzbe,
                            NarudzbaID = narudzba.NarudzbaID
                        };

                        if (recenzija != null)
                        {
                            purchasedItem.Recenzija = new PurchasedItemsViewModel.RecenzijaViewModel
                            {
                                RecenzijaID = recenzija.RecenzijaID,
                                Ocjena = recenzija.Ocjena,
                                Komentar = recenzija.Komentar,
                                DatumRecenzije = recenzija.DatumRecenzije
                            };
                        }

                        uniqueVehicles[stavka.VoziloID] = purchasedItem;
                    }
                }
            }

            model.PurchasedItems = uniqueVehicles.Values.ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajRecenziju(int voziloId, int ocjena, string komentar)
        {
            if (ocjena < 1 || ocjena > 5)
            {
                TempData["ErrorMessage"] = _localizer["ReviewRatingRange"].Value;
                return RedirectToAction("KupljeniArtikli");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var hasPurchased = await _context.Narudzbe
                .Where(n => n.KorisnikId == user.Id)
                .SelectMany(n => n.StavkeKorpe)
                .AnyAsync(s => s.VoziloID == voziloId);

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = _localizer["ReviewPurchasedOnly"].Value;
                return RedirectToAction("KupljeniArtikli");
            }

            var existingReview = await _context.Recenzije
                .FirstOrDefaultAsync(r => r.KorisnikId == user.Id && r.VoziloID == voziloId);

            if (existingReview != null)
            {
                existingReview.Ocjena = ocjena;
                existingReview.Komentar = komentar;
                existingReview.DatumRecenzije = DateTime.Now;

                _context.Recenzije.Update(existingReview);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = _localizer["ReviewUpdatedSuccess"].Value;
            }
            else
            {
                var recenzija = new Recenzija
                {
                    KorisnikId = user.Id,
                    VoziloID = voziloId,
                    Ocjena = ocjena,
                    Komentar = komentar,
                    DatumRecenzije = DateTime.Now
                };

                _context.Recenzije.Add(recenzija);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = _localizer["ReviewAddedSuccess"].Value;
            }

            return RedirectToAction("KupljeniArtikli");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UkloniRecenziju(int recenzijaId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronaden.");
            }

            var recenzija = await _context.Recenzije
                .FirstOrDefaultAsync(r => r.RecenzijaID == recenzijaId && r.KorisnikId == user.Id);

            if (recenzija == null)
            {
                TempData["ErrorMessage"] = _localizer["ReviewNotFoundOrNotOwned"].Value;
                return RedirectToAction("KupljeniArtikli");
            }

            _context.Recenzije.Remove(recenzija);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer["ReviewRemovedSuccess"].Value;
            return RedirectToAction("KupljeniArtikli");
        }
    }
}
