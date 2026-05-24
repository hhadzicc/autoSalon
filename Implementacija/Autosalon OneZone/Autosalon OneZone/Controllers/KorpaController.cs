using Autosalon_OneZone.Models;
using Autosalon_OneZone.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Autosalon_OneZone.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;

namespace Autosalon_OneZone.Controllers
{
    [Authorize]
    public class KorpaController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<KorpaController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public KorpaController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<KorpaController> logger,
            IPaymentService paymentService,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _paymentService = paymentService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajUKorpu(int id)
        {
            try
            {
                _logger.LogInformation($"Početak dodavanja vozila ID: {id} u korpu");

                var vozilo = await _context.Vozila.FindAsync(id);
                if (vozilo == null)
                {
                    _logger.LogWarning($"Vozilo sa ID: {id} nije pronađeno");
                    TempData["ErrorMessage"] = _localizer["VehicleNotFound"].Value;
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/Vozilo");
                }

                decimal cijenaVozila = vozilo.Cijena ?? 0;
                _logger.LogInformation($"Cijena vozila: {cijenaVozila}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Korisnik nije prijavljen iako je zaštićeno sa [Authorize]");
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = Url.Action("Index", "Vozilo") });
                }

                _logger.LogInformation($"Korisnik je prijavljen, ID: {user.Id}");

                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa == null)
                {
                    _logger.LogInformation("Korisnik nema korpu, kreiram novu");

                    korpa = new Korpa
                    {
                        KorisnikId = user.Id,
                        UkupnaCijena = 0
                    };

                    _context.Korpe.Add(korpa);
                    await _context.SaveChangesAsync();

                    korpa = await _context.Korpe.FirstOrDefaultAsync(k => k.KorisnikId == user.Id);
                    _logger.LogInformation($"Kreirana nova korpa, ID: {korpa.KorpaID}");
                }

                var stavkaKorpe = await _context.StavkeKorpe
                    .FirstOrDefaultAsync(s => s.KorpaID == korpa.KorpaID && s.VoziloID == id);

                if (stavkaKorpe == null)
                {
                    _logger.LogInformation($"Dodajem novo vozilo u korpu ID: {korpa.KorpaID}");

                    var novaStavka = new StavkaKorpe
                    {
                        KorpaID = korpa.KorpaID,
                        VoziloID = id,
                        Kolicina = 1,
                        CijenaStavke = cijenaVozila
                    };

                    _context.StavkeKorpe.Add(novaStavka);

                    korpa.UkupnaCijena += cijenaVozila;
                    _context.Korpe.Update(korpa);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vozilo dodano u korpu, nova ukupna cijena: {korpa.UkupnaCijena}");

                    TempData["SuccessMessage"] = _localizer["CartAddVehicleSuccess"].Value;
                }
                else
                {
                    _logger.LogInformation("Vozilo je već u korpi");
                    TempData["SuccessMessage"] = _localizer["CartVehicleAlreadyAdded"].Value;
                }

                return Redirect(Request.Headers["Referer"].ToString() ?? "/Vozilo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri dodavanju vozila u korpu: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartAddError"].Value;
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Vozilo");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                List<CartItemViewModel> stavkeKorpe = new List<CartItemViewModel>();
                decimal ukupnaCijena = 0;

                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .ThenInclude(s => s.Vozilo)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa != null && korpa.StavkeKorpe != null)
                {
                    _logger.LogInformation($"Pronađena korpa sa {korpa.StavkeKorpe.Count} stavki");

                    stavkeKorpe = korpa.StavkeKorpe.Select(s => new CartItemViewModel
                    {
                        Id = s.VoziloID,
                        StavkaId = s.StavkaID,
                        Naziv = $"{s.Vozilo.Marka} {s.Vozilo.Model}",
                        SlikaUrl = !string.IsNullOrEmpty(s.Vozilo.Slika) ? $"/images/vozila/{s.Vozilo.Slika}" : "/img/no-image.png",
                        Godiste = s.Vozilo.Godiste ?? 0,
                        Gorivo = s.Vozilo.Gorivo.ToString(),
                        Cijena = s.CijenaStavke,
                        Kolicina = s.Kolicina
                    }).ToList();

                    ukupnaCijena = korpa.UkupnaCijena;
                }

                var model = new CartViewModel
                {
                    VozilaUKorpi = stavkeKorpe,
                    UkupnaCijena = ukupnaCijena
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri prikazivanju korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartDisplayError"].Value;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IzvrsiPlacanje(int VoziloID, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            var errors = new Dictionary<string, string>();

            try
            {
                if (string.IsNullOrWhiteSpace(ImeVlasnika))
                {
                    errors.Add("imeVlasnika", _localizer["PaymentNameRequired"].Value);
                }
                else if (!ImeVlasnika.Trim().Contains(" "))
                {
                    errors.Add("imeVlasnika", _localizer["PaymentNameFullRequired"].Value);
                }

                string cleanCardNumber = string.Empty;
                if (string.IsNullOrWhiteSpace(BrojKartice))
                {
                    errors.Add("brojKartice", _localizer["PaymentCardRequired"].Value);
                }
                else
                {
                    cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                    if (cleanCardNumber.Length != 16)
                    {
                        errors.Add("brojKartice", _localizer["PaymentCardLength"].Value);
                    }
                }

                if (string.IsNullOrWhiteSpace(DatumIsteka))
                {
                    errors.Add("datumIsteka", _localizer["PaymentExpiryRequired"].Value);
                }
                else
                {
                    var dateParts = DatumIsteka.Split('/');
                    if (dateParts.Length != 2)
                    {
                        errors.Add("datumIsteka", _localizer["PaymentExpiryFormat"].Value);
                    }
                    else
                    {
                        if (!int.TryParse(dateParts[0], out int monthVal) || monthVal < 1 || monthVal > 12)
                        {
                            errors.Add("datumIsteka", _localizer["PaymentExpiryMonth"].Value);
                        }

                        if (!int.TryParse(dateParts[1], out int yearVal))
                        {
                            errors.Add("datumIsteka", _localizer["PaymentExpiryYear"].Value);
                        }
                        else
                        {
                            int fullYear = 2000 + yearVal;
                            var now = DateTime.Now;
                            if (fullYear < now.Year || (fullYear == now.Year && monthVal < now.Month))
                            {
                                errors.Add("datumIsteka", _localizer["PaymentCardExpired"].Value);
                            }
                        }
                    }
                }

                string cleanCvv = string.Empty;
                if (string.IsNullOrWhiteSpace(Cvv))
                {
                    errors.Add("cvv", _localizer["PaymentCvvRequired"].Value);
                }
                else
                {
                    cleanCvv = new string(Cvv.Where(char.IsDigit).ToArray());
                    if (cleanCvv.Length != 3)
                    {
                        errors.Add("cvv", _localizer["PaymentCvvLength"].Value);
                    }
                }

                if (errors.Any())
                {
                    return Json(new { success = false, errors });
                }

                var parts = DatumIsteka.Split('/');
                string month = parts[0];
                string year = "20" + parts[1];

                var vozilo = await _context.Vozila.FindAsync(VoziloID);
                if (vozilo == null)
                {
                    return Json(new { success = false, message = _localizer["VehicleNotFound"].Value });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = _localizer["UserNotFound"].Value });
                }

                var cijenaVozila = vozilo.Cijena ?? 0;
                if (cijenaVozila <= 0)
                {
                    return Json(new { success = false, message = _localizer["VehiclePriceInvalid"].Value });
                }

                var paymentRequest = new PaymentRequest
                {
                    CardNumber = cleanCardNumber,
                    ExpirationMonth = month,
                    ExpirationYear = year,
                    Cvv = cleanCvv,
                    Amount = cijenaVozila,
                    CustomerName = ImeVlasnika.Trim(),
                    Email = user.Email,
                    Description = $"Purchase of {vozilo.Marka} {vozilo.Model}",
                    ProductId = VoziloID
                };

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (result.Success)
                {
                    var narudzba = new Narudzba
                    {
                        KorisnikId = user.Id,
                        DatumNarudzbe = DateTime.Now,
                        UkupnaCijena = cijenaVozila,
                        Status = StatusNarudzbe.Placena
                    };

                    _context.Narudzbe.Add(narudzba);
                    await _context.SaveChangesAsync();

                    var stavka = new StavkaKorpe
                    {
                        VoziloID = VoziloID,
                        Kolicina = 1,
                        CijenaStavke = cijenaVozila,
                        NarudzbaID = narudzba.NarudzbaID
                    };

                    _context.StavkeKorpe.Add(stavka);

                    var placanje = new Placanje
                    {
                        NarudzbaID = narudzba.NarudzbaID,
                        DatumPlacanja = DateTime.Now,
                        Iznos = cijenaVozila,
                        Status = StatusPlacanja.Uspjesno,
                        KarticaID = null
                    };

                    _context.Placanja.Add(placanje);

                    var userCart = await _context.Korpe
                        .Include(k => k.StavkeKorpe)
                        .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                    if (userCart != null)
                    {
                        var cartItem = userCart.StavkeKorpe.FirstOrDefault(s => s.VoziloID == VoziloID);
                        if (cartItem != null)
                        {
                            _context.StavkeKorpe.Remove(cartItem);

                            userCart.UkupnaCijena -= cartItem.CijenaStavke;
                            if (userCart.UkupnaCijena < 0)
                            {
                                userCart.UkupnaCijena = 0;
                            }

                            _context.Korpe.Update(userCart);
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Uspjesno izvrseno placanje za vozilo ID: {VoziloID}, iznos: {Iznos}, korisnik: {KorisnikId}", VoziloID, cijenaVozila, user.Id);

                    return Json(new
                    {
                        success = true,
                        message = _localizer["PaymentOrderRecorded"].Value,
                        redirectUrl = Url.Action("Uspjeh", "Korpa", new { id = narudzba.NarudzbaID })
                    });
                }

                _logger.LogWarning("Neuspjesno placanje za vozilo ID: {VoziloID}, iznos: {Iznos}, korisnik: {KorisnikId}, razlog: {Razlog}", VoziloID, cijenaVozila, user.Id, result.Message);
                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska prilikom izvrsavanja placanja: {Message}", ex.Message);
                return Json(new { success = false, message = _localizer["PaymentProcessingError"].Value });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> IzvrsiPlacanjeSvih(string OdabranaVozilaJSON, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            var errors = new Dictionary<string, string>();

            try
            {
                _logger.LogInformation("Početak obrade grupnog plaćanja");
                _logger.LogInformation("Primljeni JSON: {OdabranaVozilaJSON}", OdabranaVozilaJSON);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                List<OdabranoVoziloViewModel>? odabranaVozila = null;
                if (!string.IsNullOrWhiteSpace(OdabranaVozilaJSON))
                {
                    try
                    {
                        odabranaVozila = JsonSerializer.Deserialize<List<OdabranoVoziloViewModel>>(OdabranaVozilaJSON, options);
                    }
                    catch (JsonException)
                    {
                        return Json(new { success = false, message = _localizer["NoVehiclesSelectedForPurchase"].Value });
                    }
                }

                if (odabranaVozila == null || !odabranaVozila.Any())
                {
                    return Json(new { success = false, message = _localizer["NoVehiclesSelectedForPurchase"].Value });
                }

                _logger.LogInformation("Broj odabranih vozila: {Count}", odabranaVozila.Count);

                if (string.IsNullOrWhiteSpace(ImeVlasnika))
                {
                    errors.Add("checkoutImeVlasnika", _localizer["PaymentNameRequired"].Value);
                }
                else if (!ImeVlasnika.Contains(" "))
                {
                    errors.Add("checkoutImeVlasnika", _localizer["PaymentNameFullRequired"].Value);
                }

                string cleanCardNumber = string.Empty;
                if (string.IsNullOrWhiteSpace(BrojKartice))
                {
                    errors.Add("checkoutBrojKartice", _localizer["PaymentCardRequired"].Value);
                }
                else
                {
                    cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                    if (cleanCardNumber.Length != 16)
                    {
                        errors.Add("checkoutBrojKartice", _localizer["PaymentCardLength"].Value);
                    }
                }

                if (string.IsNullOrWhiteSpace(DatumIsteka))
                {
                    errors.Add("checkoutDatumIsteka", _localizer["PaymentExpiryRequired"].Value);
                }
                else
                {
                    var dateParts = DatumIsteka.Split('/');
                    if (dateParts.Length != 2)
                    {
                        errors.Add("checkoutDatumIsteka", _localizer["PaymentExpiryFormat"].Value);
                    }
                    else
                    {
                        if (!int.TryParse(dateParts[0], out int monthVal) || monthVal < 1 || monthVal > 12)
                        {
                            errors.Add("checkoutDatumIsteka", _localizer["PaymentExpiryMonth"].Value);
                        }

                        if (!int.TryParse(dateParts[1], out int yearVal))
                        {
                            errors.Add("checkoutDatumIsteka", _localizer["PaymentExpiryYear"].Value);
                        }
                        else
                        {
                            int fullYear = 2000 + yearVal;
                            var now = DateTime.Now;
                            if (fullYear < now.Year || (fullYear == now.Year && monthVal < now.Month))
                            {
                                errors.Add("checkoutDatumIsteka", _localizer["PaymentCardExpired"].Value);
                            }
                        }
                    }
                }

                string cleanCvv = string.Empty;
                if (string.IsNullOrWhiteSpace(Cvv))
                {
                    errors.Add("checkoutCvv", _localizer["PaymentCvvRequired"].Value);
                }
                else
                {
                    cleanCvv = new string(Cvv.Where(char.IsDigit).ToArray());
                    if (cleanCvv.Length != 3)
                    {
                        errors.Add("checkoutCvv", _localizer["PaymentCvvLength"].Value);
                    }
                }

                if (errors.Any())
                {
                    return Json(new { success = false, errors });
                }

                var parts = DatumIsteka.Split('/');
                string month = parts[0];
                string year = "20" + parts[1];

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = _localizer["UserNotFound"].Value });
                }

                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .ThenInclude(s => s.Vozilo)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa == null)
                {
                    return Json(new { success = false, message = _localizer["CartNotFound"].Value });
                }

                var odabraniVozilaIds = odabranaVozila.Select(v => v.id).ToHashSet();
                _logger.LogInformation("Odabrana vozila IDs: {IDs}", string.Join(", ", odabraniVozilaIds));

                foreach (var voziloId in odabraniVozilaIds)
                {
                    if (!korpa.StavkeKorpe.Any(s => s.VoziloID == voziloId))
                    {
                        return Json(new { success = false, message = _localizer["SelectedVehicleMissingFromCart"].Value });
                    }
                }

                decimal ukupnaCijena = korpa.StavkeKorpe
                    .Where(s => odabraniVozilaIds.Contains(s.VoziloID))
                    .Sum(s => s.CijenaStavke);

                var paymentRequest = new PaymentRequest
                {
                    CardNumber = cleanCardNumber,
                    ExpirationMonth = month,
                    ExpirationYear = year,
                    Cvv = cleanCvv,
                    Amount = ukupnaCijena,
                    CustomerName = ImeVlasnika,
                    Email = user.Email,
                    Description = $"Grupna kupovina {odabraniVozilaIds.Count} vozila",
                    ProductId = 0
                };

                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (result.Success)
                {
                    var narudzba = new Narudzba
                    {
                        KorisnikId = user.Id,
                        DatumNarudzbe = DateTime.Now,
                        UkupnaCijena = ukupnaCijena,
                        Status = StatusNarudzbe.Placena
                    };

                    _context.Narudzbe.Add(narudzba);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Kreirana zajednička narudžba, ID: {NarudzbaID}", narudzba.NarudzbaID);

                    string maskicaniBrojKartice = cleanCardNumber;
                    if (maskicaniBrojKartice.Length >= 4)
                    {
                        maskicaniBrojKartice = maskicaniBrojKartice.Substring(maskicaniBrojKartice.Length - 4).PadLeft(maskicaniBrojKartice.Length, '*');
                    }

                    var kartica = new Kartica
                    {
                        BrojKartice = maskicaniBrojKartice,
                        DatumIsteka = DatumIsteka,
                        ImeVlasnika = ImeVlasnika,
                        Cvv = "***"
                    };

                    _context.Kartice.Add(kartica);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Kreirana kartica, ID: {KarticaID}", kartica.KarticaID);

                    var placanje = new Placanje
                    {
                        NarudzbaID = narudzba.NarudzbaID,
                        DatumPlacanja = DateTime.Now,
                        Iznos = ukupnaCijena,
                        Status = StatusPlacanja.Uspjesno,
                        KarticaID = kartica.KarticaID,
                        KreditID = null
                    };

                    _context.Placanja.Add(placanje);
                    _logger.LogInformation("Kreirano jedno plaćanje za cijelu narudžbu");

                    List<int> kupljenaVozilaIds = new List<int>();
                    List<StavkaKorpe> stavkeZaAzuriranje = new List<StavkaKorpe>();

                    foreach (var stavkaKorpe in korpa.StavkeKorpe.ToList())
                    {
                        if (odabraniVozilaIds.Contains(stavkaKorpe.VoziloID))
                        {
                            _logger.LogInformation("Procesiranje odabrane stavke korpe, VoziloID: {VoziloID}", stavkaKorpe.VoziloID);

                            stavkaKorpe.KorpaID = null;
                            stavkaKorpe.NarudzbaID = narudzba.NarudzbaID;
                            stavkeZaAzuriranje.Add(stavkaKorpe);

                            korpa.UkupnaCijena -= stavkaKorpe.CijenaStavke;
                            kupljenaVozilaIds.Add(stavkaKorpe.VoziloID);
                        }
                    }

                    foreach (var stavka in stavkeZaAzuriranje)
                    {
                        _context.StavkeKorpe.Update(stavka);
                    }

                    if (korpa.UkupnaCijena < 0)
                        korpa.UkupnaCijena = 0;

                    _context.Korpe.Update(korpa);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Sve promjene uspješno sačuvane");

                    return Json(new
                    {
                        success = true,
                        message = string.Format(_localizer["MultiVehiclePurchaseSuccess"].Value, kupljenaVozilaIds.Count),
                        redirectUrl = Url.Action("Uspjeh", "Korpa", new { id = narudzba.NarudzbaID })
                    });
                }
                else
                {
                    _logger.LogWarning("Neuspješno grupno plaćanje, razlog: {Message}", result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stvarna greška prilikom obrade grupnog plaćanja: {Message}", ex.Message);
                return Json(new { success = false, message = _localizer["PaymentProcessingError"].Value });
            }
        }

        public async Task<IActionResult> Uspjeh(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var narudzbeQuery = _context.Narudzbe
                .Include(n => n.StavkeKorpe)
                .ThenInclude(s => s.Vozilo)
                .AsQueryable();

            if (!User.IsInRole("Administrator"))
            {
                narudzbeQuery = narudzbeQuery.Where(n => n.KorisnikId == user.Id);
            }

            var narudzba = await narudzbeQuery.FirstOrDefaultAsync(n => n.NarudzbaID == id);

            if (narudzba == null)
            {
                return Forbid();
            }

            return View(narudzba);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UkloniIzKorpe(int id)
        {
            try
            {
                _logger.LogInformation($"Uklanjanje stavke iz korpe, ID: {id}");
                var user = await _userManager.GetUserAsync(User);

                var korpa = await _context.Korpe
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa != null)
                {
                    var stavka = await _context.StavkeKorpe
                        .FirstOrDefaultAsync(s => s.VoziloID == id && s.KorpaID == korpa.KorpaID);

                    if (stavka != null)
                    {
                        _logger.LogInformation($"Pronađena stavka za brisanje, ID: {stavka.StavkaID}, Cijena: {stavka.CijenaStavke}");

                        korpa.UkupnaCijena -= (stavka.CijenaStavke * stavka.Kolicina);
                        if (korpa.UkupnaCijena < 0) korpa.UkupnaCijena = 0;

                        _context.Korpe.Update(korpa);
                        _context.StavkeKorpe.Remove(stavka);

                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Stavka uklonjena, nova ukupna cijena: {korpa.UkupnaCijena}");

                        TempData["SuccessMessage"] = _localizer["CartRemoveSuccess"].Value;
                    }
                    else
                    {
                        _logger.LogWarning($"Stavka nije pronađena za VoziloID: {id} u KorpaID: {korpa.KorpaID}");
                        TempData["InfoMessage"] = _localizer["CartVehicleNotFoundInCart"].Value;
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri uklanjanju vozila iz korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartRemoveError"].Value;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcistiKorpu()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa != null)
                {
                    _context.StavkeKorpe.RemoveRange(korpa.StavkeKorpe);

                    korpa.UkupnaCijena = 0;
                    _context.Korpe.Update(korpa);

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = _localizer["CartClearedSuccess"].Value;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri čišćenju korpe: {ex.Message}");
                TempData["ErrorMessage"] = _localizer["CartClearError"].Value;
                return RedirectToAction("Index");
            }
        }
    }
}
