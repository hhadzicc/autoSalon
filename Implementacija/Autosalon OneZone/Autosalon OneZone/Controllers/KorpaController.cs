// Controllers/KorpaController.cs
using Autosalon_OneZone.Data.Helpers;
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
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace Autosalon_OneZone.Controllers
{
    [Authorize] // Zahtijeva autentikaciju za sve akcije u kontroleru
    public class KorpaController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly StripeSettings _stripeSettings;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<KorpaController> _logger;

        public KorpaController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<KorpaController> logger, IPaymentService paymentService, IOptions<StripeSettings> stripeSettings)
        {
            _paymentService = paymentService;
            _stripeSettings = stripeSettings.Value;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajUKorpu(int id)
        {
            try
            {
                _logger.LogInformation($"Početak dodavanja vozila ID: {id} u korpu");

                // Pronađi vozilo u bazi
                var vozilo = await _context.Vozila.FindAsync(id);
                if (vozilo == null)
                {
                    _logger.LogWarning($"Vozilo sa ID: {id} nije pronađeno");
                    TempData["ErrorMessage"] = "Vozilo nije pronađeno.";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/Vozilo");
                }

                decimal cijenaVozila = vozilo.Cijena ?? 0;
                _logger.LogInformation($"Cijena vozila: {cijenaVozila}");

                // Dohvati trenutno prijavljenog korisnika
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Ova linija se ne bi trebala izvršiti zbog [Authorize] atributa,
                    // ali dodatna je provjera
                    _logger.LogWarning("Korisnik nije prijavljen iako je zaštićeno sa [Authorize]");
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = Url.Action("Index", "Vozilo") });
                }

                _logger.LogInformation($"Korisnik je prijavljen, ID: {user.Id}");

                // Prvo provjerimo ima li korisnik već korpu
                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa == null)
                {
                    _logger.LogInformation("Korisnik nema korpu, kreiram novu");

                    // Kreiraj novu korpu za korisnika
                    korpa = new Korpa
                    {
                        KorisnikId = user.Id,
                        UkupnaCijena = 0 // Inicijalna ukupna cijena je 0
                    };

                    _context.Korpe.Add(korpa);
                    await _context.SaveChangesAsync();

                    // Ponovno dohvati korpu sa ID-em koji je generirala baza
                    korpa = await _context.Korpe.FirstOrDefaultAsync(k => k.KorisnikId == user.Id);
                    _logger.LogInformation($"Kreirana nova korpa, ID: {korpa.KorpaID}");
                }

                // Provjeri ima li već vozilo u korpi
                var stavkaKorpe = await _context.StavkeKorpe
                    .FirstOrDefaultAsync(s => s.KorpaID == korpa.KorpaID && s.VoziloID == id);

                if (stavkaKorpe == null)
                {
                    _logger.LogInformation($"Dodajem novo vozilo u korpu ID: {korpa.KorpaID}");

                    // Dodaj novu stavku u korpu
                    var novaStavka = new StavkaKorpe
                    {
                        KorpaID = korpa.KorpaID,
                        VoziloID = id,
                        Kolicina = 1,
                        CijenaStavke = cijenaVozila
                    };

                    _context.StavkeKorpe.Add(novaStavka);

                    // Ažuriraj ukupnu cijenu korpe
                    korpa.UkupnaCijena += cijenaVozila;
                    _context.Korpe.Update(korpa);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Vozilo dodano u korpu, nova ukupna cijena: {korpa.UkupnaCijena}");

                    TempData["SuccessMessage"] = "Vozilo je uspješno dodato u korpu!";
                }
                else
                {
                    _logger.LogInformation("Vozilo je već u korpi");
                    TempData["SuccessMessage"] = "Vozilo je već dodato u korpu.";
                }

                return Redirect(Request.Headers["Referer"].ToString() ?? "/Vozilo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri dodavanju vozila u korpu: {ex.Message}");
                TempData["SuccessMessage"] = "Došlo je do greške pri dodavanju vozila u korpu. Molimo pokušajte ponovo.";
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

                // Za prijavljenog korisnika, dohvati korpu iz baze
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
                TempData["ErrorMessage"] = "Došlo je do greške pri prikazivanju korpe. Molimo pokušajte ponovo.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IzvrsiPlacanje(int VoziloID, decimal Cijena, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            // Create a dictionary to hold validation errors
            var errors = new Dictionary<string, string>();

            try
            {
                // Validate card owner name
                if (string.IsNullOrWhiteSpace(ImeVlasnika))
                {
                    errors.Add("imeVlasnika", "Ime i prezime su obavezni.");
                }
                else if (!ImeVlasnika.Contains(" "))
                {
                    errors.Add("imeVlasnika", "Unesite i ime i prezime (mora sadržavati razmak).");
                }

                // Validate card number
                string cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cleanCardNumber))
                {
                    errors.Add("brojKartice", "Broj kartice je obavezan.");
                }
                else if (cleanCardNumber.Length != 16)
                {
                    errors.Add("brojKartice", "Broj kartice mora sadržavati tačno 16 cifara.");
                }

                // Validate expiration date
                if (string.IsNullOrWhiteSpace(DatumIsteka))
                {
                    errors.Add("datumIsteka", "Datum isteka je obavezan.");
                }
                else
                {
                    var dateParts = DatumIsteka.Split('/');
                    if (dateParts.Length != 2)
                    {
                        errors.Add("datumIsteka", "Neispravan format datuma isteka. Koristite format MM/YY.");
                    }
                    else
                    {
                        // Use different variable names in validation to avoid scope conflicts
                        if (!int.TryParse(dateParts[0], out int monthVal) || monthVal < 1 || monthVal > 12)
                        {
                            errors.Add("datumIsteka", "Mjesec mora biti između 01 i 12.");
                        }

                        if (!int.TryParse(dateParts[1], out int yearVal))
                        {
                            errors.Add("datumIsteka", "Godina nije ispravna.");
                        }
                        else
                        {
                            int fullYear = 2000 + yearVal;
                            var now = DateTime.Now;
                            if (fullYear < now.Year || (fullYear == now.Year && monthVal < now.Month))
                            {
                                errors.Add("datumIsteka", "Kartica je istekla.");
                            }
                        }
                    }
                }

                // Validate CVV
                string cleanCvv = new string(Cvv.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cleanCvv))
                {
                    errors.Add("cvv", "CVV kod je obavezan.");
                }
                else if (cleanCvv.Length != 3)
                {
                    errors.Add("cvv", "CVV kod mora sadržavati tačno 3 cifre.");
                }

                // If there are any validation errors, return them to the client
                if (errors.Any())
                {
                    return Json(new { success = false, errors });
                }

                // Parse expiration date for payment processing
                var parts = DatumIsteka.Split('/');
                string month = parts[0];
                string year = "20" + parts[1]; // Convert YY to YYYY

                // Get the vehicle for description
                var vozilo = await _context.Vozila.FindAsync(VoziloID);
                if (vozilo == null)
                {
                    return Json(new { success = false, message = "Vozilo nije pronađeno." });
                }

                // Get current user's email
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Korisnik nije pronađen." });
                }

                // Create payment request
                var paymentRequest = new PaymentRequest
                {
                    CardNumber = cleanCardNumber,
                    ExpirationMonth = month,
                    ExpirationYear = year,
                    Cvv = cleanCvv,
                    Amount = Cijena,
                    CustomerName = ImeVlasnika,
                    Email = user.Email,
                    Description = $"Purchase of {vozilo.Marka} {vozilo.Model}",
                    ProductId = VoziloID
                };

                // Process payment
                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (result.Success)
                {
                    // Create order
                    var narudzba = new Narudzba
                    {
                        KorisnikId = user.Id,
                        DatumNarudzbe = DateTime.Now,
                        UkupnaCijena = Cijena,
                        Status = StatusNarudzbe.Placena
                    };

                    _context.Narudzbe.Add(narudzba);
                    await _context.SaveChangesAsync();

                    // Add order item
                    var stavka = new StavkaKorpe
                    {
                        VoziloID = VoziloID,
                        Kolicina = 1,
                        CijenaStavke = Cijena,
                        NarudzbaID = narudzba.NarudzbaID
                    };

                    _context.StavkeKorpe.Add(stavka);

                    // Create payment record
                    var placanje = new Placanje
                    {
                        NarudzbaID = narudzba.NarudzbaID,
                        DatumPlacanja = DateTime.Now,
                        Iznos = Cijena,
                        Status = StatusPlacanja.Uspjesno,
                        KarticaID = null // You might want to store the masked card details
                    };

                    _context.Placanja.Add(placanje);

                    // Remove item from cart if it was there
                    var userCart = await _context.Korpe
                        .Include(k => k.StavkeKorpe)
                        .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                    if (userCart != null)
                    {
                        var cartItem = userCart.StavkeKorpe.FirstOrDefault(s => s.VoziloID == VoziloID);
                        if (cartItem != null)
                        {
                            // Remove item from cart
                            _context.StavkeKorpe.Remove(cartItem);

                            // Update cart total
                            userCart.UkupnaCijena -= cartItem.CijenaStavke;
                            if (userCart.UkupnaCijena < 0)
                                userCart.UkupnaCijena = 0;

                            _context.Korpe.Update(userCart);
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Uspješno izvršeno plaćanje za vozilo ID: {VoziloID}, iznos: {Cijena}, korisnik: {user.Id}");

                    return Json(new
                    {
                        success = true,
                        message = "Plaćanje uspješno izvršeno. Vaša narudžba je evidentirana.",
                        redirectUrl = Url.Action("Uspjeh", "Korpa", new { id = narudzba.NarudzbaID })
                    });
                }
                else
                {
                    _logger.LogWarning($"Neuspješno plaćanje za vozilo ID: {VoziloID}, iznos: {Cijena}, korisnik: {user.Id}, razlog: {result.Message}");

                    // Payment failed with Stripe
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška prilikom izvršavanja plaćanja: {ex.Message}");
                return Json(new { success = false, message = "Došlo je do greške prilikom obrade plaćanja. Molimo pokušajte ponovo." });
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> IzvrsiPlacanjeSvih(string OdabranaVozilaJSON, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            // Create a dictionary to hold validation errors
            var errors = new Dictionary<string, string>();

            try
            {
                _logger.LogInformation("Početak obrade grupnog plaćanja");
                _logger.LogInformation("Primljeni JSON: {OdabranaVozilaJSON}", OdabranaVozilaJSON);

                // Koristimo posebne opcije za deserijalizaciju
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                // Dekodiranje JSON podataka
                var odabranaVozila = JsonSerializer.Deserialize<List<OdabranoVoziloViewModel>>(OdabranaVozilaJSON, options);

                if (odabranaVozila == null || !odabranaVozila.Any())
                {
                    return Json(new { success = false, message = "Niste odabrali nijedno vozilo za kupovinu." });
                }

                _logger.LogInformation("Broj odabranih vozila: {Count}", odabranaVozila.Count);

                // Validate card owner name
                if (string.IsNullOrWhiteSpace(ImeVlasnika))
                {
                    errors.Add("checkoutImeVlasnika", "Ime i prezime su obavezni.");
                }
                else if (!ImeVlasnika.Contains(" "))
                {
                    errors.Add("checkoutImeVlasnika", "Unesite i ime i prezime (mora sadržavati razmak).");
                }

                // Validate card number
                string cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cleanCardNumber))
                {
                    errors.Add("checkoutBrojKartice", "Broj kartice je obavezan.");
                }
                else if (cleanCardNumber.Length != 16)
                {
                    errors.Add("checkoutBrojKartice", "Broj kartice mora sadržavati tačno 16 cifara.");
                }

                // Validate expiration date
                if (string.IsNullOrWhiteSpace(DatumIsteka))
                {
                    errors.Add("checkoutDatumIsteka", "Datum isteka je obavezan.");
                }
                else
                {
                    var dateParts = DatumIsteka.Split('/');
                    if (dateParts.Length != 2)
                    {
                        errors.Add("checkoutDatumIsteka", "Neispravan format datuma isteka. Koristite format MM/YY.");
                    }
                    else
                    {
                        // Use different variable names in validation to avoid scope conflicts
                        if (!int.TryParse(dateParts[0], out int monthVal) || monthVal < 1 || monthVal > 12)
                        {
                            errors.Add("checkoutDatumIsteka", "Mjesec mora biti između 01 i 12.");
                        }

                        if (!int.TryParse(dateParts[1], out int yearVal))
                        {
                            errors.Add("checkoutDatumIsteka", "Godina nije ispravna.");
                        }
                        else
                        {
                            int fullYear = 2000 + yearVal;
                            var now = DateTime.Now;
                            if (fullYear < now.Year || (fullYear == now.Year && monthVal < now.Month))
                            {
                                errors.Add("checkoutDatumIsteka", "Kartica je istekla.");
                            }
                        }
                    }
                }

                // Validate CVV
                string cleanCvv = new string(Cvv.Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cleanCvv))
                {
                    errors.Add("checkoutCvv", "CVV kod je obavezan.");
                }
                else if (cleanCvv.Length != 3)
                {
                    errors.Add("checkoutCvv", "CVV kod mora sadržavati tačno 3 cifre.");
                }

                // If there are any validation errors, return them to the client
                if (errors.Any())
                {
                    return Json(new { success = false, errors });
                }

                // Parse expiration date for payment processing
                var parts = DatumIsteka.Split('/');
                string month = parts[0];
                string year = "20" + parts[1]; // Convert YY to YYYY

                // Dohvati trenutno prijavljenog korisnika
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Korisnik nije pronađen." });
                }

                // Dohvati korpu korisnika sa svim stavkama
                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .ThenInclude(s => s.Vozilo)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa == null)
                {
                    return Json(new { success = false, message = "Korpa nije pronađena." });
                }

                // Kreiraj set ID-ova odabranih vozila za brže poređenje
                var odabraniVozilaIds = odabranaVozila.Select(v => v.id).ToHashSet();
                _logger.LogInformation("Odabrana vozila IDs: {IDs}", string.Join(", ", odabraniVozilaIds));

                // Provjeri da li sva odabrana vozila postoje u korpi
                foreach (var voziloId in odabraniVozilaIds)
                {
                    if (!korpa.StavkeKorpe.Any(s => s.VoziloID == voziloId))
                    {
                        return Json(new { success = false, message = "Jedno ili više odabranih vozila nije pronađeno u vašoj korpi." });
                    }
                }

                // Izračunaj ukupnu cijenu odabranih vozila
                decimal ukupnaCijena = korpa.StavkeKorpe
                    .Where(s => odabraniVozilaIds.Contains(s.VoziloID))
                    .Sum(s => s.CijenaStavke);

                // Create payment request for the total amount
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
                    ProductId = 0 // Group purchase has no single product ID
                };

                // Process payment using the payment service
                var result = await _paymentService.ProcessPaymentAsync(paymentRequest);

                if (result.Success)
                {
                    // Kreiramo zajedničku narudžbu za sva vozila
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

                    // Kreiramo karticu jednom za sve transakcije
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

                    // Kreiramo JEDNO plaćanje za cijelu narudžbu (ovo rješava problem)
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

                    // Lista za praćenje kupljenih vozila i stavki za ažuriranje
                    List<int> kupljenaVozilaIds = new List<int>();
                    List<StavkaKorpe> stavkeZaAzuriranje = new List<StavkaKorpe>();

                    // Pronađi stavke korpe koje odgovaraju odabranim vozilima
                    foreach (var stavkaKorpe in korpa.StavkeKorpe.ToList())
                    {
                        // Provjeri da li je vozilo iz ove stavke među odabranim vozilima
                        if (odabraniVozilaIds.Contains(stavkaKorpe.VoziloID))
                        {
                            _logger.LogInformation("Procesiranje odabrane stavke korpe, VoziloID: {VoziloID}", stavkaKorpe.VoziloID);

                            // Prebaci stavku iz korpe u narudžbu
                            stavkaKorpe.KorpaID = null;
                            stavkaKorpe.NarudzbaID = narudzba.NarudzbaID;
                            stavkeZaAzuriranje.Add(stavkaKorpe);

                            // Ažuriraj ukupnu cijenu korpe
                            korpa.UkupnaCijena -= stavkaKorpe.CijenaStavke;
                            kupljenaVozilaIds.Add(stavkaKorpe.VoziloID);
                        }
                    }

                    // Ažuriraj stavke umjesto da ih brišeš
                    foreach (var stavka in stavkeZaAzuriranje)
                    {
                        _context.StavkeKorpe.Update(stavka);
                    }

                    // Ažuriraj korpu
                    if (korpa.UkupnaCijena < 0)
                        korpa.UkupnaCijena = 0;

                    _context.Korpe.Update(korpa);

                    // Sačuvaj sve promjene
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Sve promjene uspješno sačuvane");

                    return Json(new
                    {
                        success = true,
                        message = $"Uspješno ste kupili {kupljenaVozilaIds.Count} vozila!",
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
                return Json(new { success = false, message = "Došlo je do greške prilikom obrade plaćanja. Molimo pokušajte ponovo." });
            }
        }





        // Validacija imena i prezimena - dozvoljava samo slova engleskog alfabeta
        private bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Provjerava da li su svi znakovi slova (dozvoljena su i razmaci između imena i prezimena)
            return name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
        }

        // Jednostavna validacija broja kartice - više ne koristimo Luhnov algoritam
        // Novu metodu IsValidCreditCardNumber nećemo koristiti, ali je zadržavam kao komentar
        // da pokažem da smo uklonili kompleksnu validaciju
        /*
        // Stara validacija sa Luhn algoritmom - više je ne koristimo
        private bool IsValidCreditCardNumber(string cardNumber)
        {
            // The Luhn Algorithm
            // https://en.wikipedia.org/wiki/Luhn_algorithm
            int[] numberArray = cardNumber.Select(c => c - '0').ToArray();
            int sum = 0;
            bool alternate = false;

            for (int i = numberArray.Length - 1; i >= 0; i--)
            {
                int n = numberArray[i];
                if (alternate)
                {
                    n *= 2;
                    if (n > 9)
                    {
                        n -= 9;
                    }
                }
                sum += n;
                alternate = !alternate;
            }

            return (sum % 10 == 0);
        }
        */

        // Validacija datuma isteka kartice
        private bool ValidateExpiryDate(string expiryDate)
        {
            // Očekivani format: MM/YY
            if (!System.Text.RegularExpressions.Regex.IsMatch(expiryDate, @"^(0[1-9]|1[0-2])\/[0-9]{2}$"))
                return false;

            string[] parts = expiryDate.Split('/');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
                return false;

            // Dodaj 2000 da bi dobio punu godinu
            year += 2000;

            // Provjeri da li je kartica istekla
            DateTime now = DateTime.Now;
            DateTime cardExpiry = new DateTime(year, month, DateTime.DaysInMonth(year, month)); // Zadnji dan mjeseca

            return cardExpiry >= new DateTime(now.Year, now.Month, 1);
        }



        // GET: /Korpa/Uspjeh/5
        public async Task<IActionResult> Uspjeh(int id)
        {
            var narudzba = await _context.Narudzbe
                .Include(n => n.StavkeKorpe)
                .ThenInclude(s => s.Vozilo)
                .FirstOrDefaultAsync(n => n.NarudzbaID == id);

            if (narudzba == null)
            {
                return NotFound();
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

                // Za prijavljenog korisnika, briši iz baze
                var korpa = await _context.Korpe
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa != null)
                {
                    var stavka = await _context.StavkeKorpe
                        .FirstOrDefaultAsync(s => s.VoziloID == id && s.KorpaID == korpa.KorpaID);

                    if (stavka != null)
                    {
                        _logger.LogInformation($"Pronađena stavka za brisanje, ID: {stavka.StavkaID}, Cijena: {stavka.CijenaStavke}");

                        // Umanjujemo ukupnu cijenu korpe
                        korpa.UkupnaCijena -= (stavka.CijenaStavke * stavka.Kolicina);
                        if (korpa.UkupnaCijena < 0) korpa.UkupnaCijena = 0; // Sigurnosna provjera

                        _context.Korpe.Update(korpa);
                        _context.StavkeKorpe.Remove(stavka);

                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Stavka uklonjena, nova ukupna cijena: {korpa.UkupnaCijena}");

                        TempData["SuccessMessage"] = "Vozilo je uklonjeno iz korpe.";
                    }
                    else
                    {
                        _logger.LogWarning($"Stavka nije pronađena za VoziloID: {id} u KorpaID: {korpa.KorpaID}");
                        TempData["InfoMessage"] = "Vozilo nije pronađeno u korpi.";
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri uklanjanju vozila iz korpe: {ex.Message}");
                TempData["ErrorMessage"] = "Došlo je do greške pri uklanjanju vozila iz korpe. Molimo pokušajte ponovo.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AzurirajKolicinu(int stavkaId, int kolicina)
        {
            try
            {
                if (kolicina < 1)
                {
                    TempData["ErrorMessage"] = "Količina ne može biti manja od 1.";
                    return RedirectToAction("Index");
                }

                var user = await _userManager.GetUserAsync(User);
                var korpa = await _context.Korpe
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa != null)
                {
                    var stavka = await _context.StavkeKorpe
                        .FirstOrDefaultAsync(s => s.StavkaID == stavkaId && s.KorpaID == korpa.KorpaID);

                    if (stavka != null)
                    {
                        _logger.LogInformation($"Ažuriranje količine stavke ID: {stavkaId}, stara kol: {stavka.Kolicina}, nova kol: {kolicina}");

                        // Računamo razliku za ukupnu cijenu
                        decimal staraVrijednost = stavka.Kolicina * stavka.CijenaStavke;
                        decimal novaVrijednost = kolicina * stavka.CijenaStavke;

                        // Ažuriraj količinu stavke
                        stavka.Kolicina = kolicina;
                        _context.StavkeKorpe.Update(stavka);

                        // Ažuriraj ukupnu cijenu korpe
                        korpa.UkupnaCijena = korpa.UkupnaCijena - staraVrijednost + novaVrijednost;
                        _context.Korpe.Update(korpa);

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Količina je ažurirana.";
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri ažuriranju količine: {ex.Message}");
                TempData["ErrorMessage"] = "Došlo je do greške pri ažuriranju količine.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            try
            {
                // Checkout je dostupan samo prijavljenim korisnicima
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri prikazivanju checkout stranice: {ex.Message}");
                TempData["ErrorMessage"] = "Došlo je do greške. Molimo pokušajte ponovo.";
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
                    // Ukloni sve stavke iz korpe
                    _context.StavkeKorpe.RemoveRange(korpa.StavkeKorpe);

                    // Resetiraj ukupnu cijenu
                    korpa.UkupnaCijena = 0;
                    _context.Korpe.Update(korpa);

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Korpa je uspješno očišćena.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Greška pri čišćenju korpe: {ex.Message}");
                TempData["ErrorMessage"] = "Došlo je do greške pri čišćenju korpe.";
                return RedirectToAction("Index");
            }
        }
    }
}
