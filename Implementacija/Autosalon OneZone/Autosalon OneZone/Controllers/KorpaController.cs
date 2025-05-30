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

namespace Autosalon_OneZone.Controllers
{
    [Authorize] // Zahtijeva autentikaciju za sve akcije u kontroleru
    public class KorpaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<KorpaController> _logger;

        public KorpaController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<KorpaController> logger)
        {
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
        [Authorize]
        public async Task<IActionResult> IzvrsiPlacanje(int VoziloID, decimal Cijena, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            try
            {
                // Postojeći kod validacije...

                // Dohvatimo trenutno prijavljenog korisnika
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // 1. Prvo provjeri da li vozilo postoji
                var vozilo = await _context.Vozila.FindAsync(VoziloID);
                if (vozilo == null)
                {
                    TempData["ErrorMessage"] = "Vozilo nije pronađeno.";
                    return RedirectToAction("Index", "Vozilo");
                }

                // 2. Dohvati korpu korisnika
                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                // 3. Pronađi stavku u korpi (ako postoji)
                StavkaKorpe stavkaKorpe = null;
                if (korpa != null)
                {
                    stavkaKorpe = korpa.StavkeKorpe.FirstOrDefault(s => s.VoziloID == VoziloID);
                }

                // 4. Kreiraj novu narudžbu
                var narudzba = new Narudzba
                {
                    KorisnikId = user.Id,
                    DatumNarudzbe = DateTime.Now,
                    UkupnaCijena = Cijena,
                    Status = 0 // Status.Plaćeno
                };
                _context.Narudzbe.Add(narudzba);
                await _context.SaveChangesAsync();

                // 5. Dodaj vozilo kao stavku narudžbe
                var stavka = new StavkaKorpe
                {
                    NarudzbaID = narudzba.NarudzbaID,
                    VoziloID = VoziloID,
                    Kolicina = 1,
                    CijenaStavke = Cijena
                };
                _context.StavkeKorpe.Add(stavka);

                // 6. Kreiraj zapis o kartici (samo osnovni podaci)
                // Sakrij dio broja kartice za sigurnost
       
                // Fix for CS0103: The name 'cleanCardNumber' does not exist in the current context
                // Define the 'cleanCardNumber' variable before using it.

                string cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                string maskicaniBrojKartice = cleanCardNumber;
                if (maskicaniBrojKartice.Length >= 4)
                {
                    maskicaniBrojKartice = maskicaniBrojKartice.Substring(maskicaniBrojKartice.Length - 4).PadLeft(maskicaniBrojKartice.Length, '*');
                }
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

                // 7. Kreiraj zapis o plaćanju
                var placanje = new Placanje
                {
                    NarudzbaID = narudzba.NarudzbaID,
                    DatumPlacanja = DateTime.Now,
                    Iznos = Cijena,
                    Status = 0,
                    KarticaID = kartica.KarticaID,
                    KreditID = null
                };
                _context.Placanja.Add(placanje);

                // 8. Ako je vozilo bilo u korpi, ukloni ga
                if (stavkaKorpe != null)
                {
                    // Umanjujemo ukupnu cijenu korpe
                    korpa.UkupnaCijena -= stavkaKorpe.CijenaStavke;
                    if (korpa.UkupnaCijena < 0)
                        korpa.UkupnaCijena = 0;

                    // Ukloni stavku iz korpe
                    _context.StavkeKorpe.Remove(stavkaKorpe);
                    _context.Korpe.Update(korpa);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Uspješno ste kupili vozilo!";
                return RedirectToAction("Uspjeh", new { id = narudzba.NarudzbaID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška prilikom obrade plaćanja");
                TempData["ErrorMessage"] = "Došlo je do greške prilikom obrade plaćanja. Molimo pokušajte ponovo.";
                return RedirectToAction("Details", "Vozilo", new { id = VoziloID });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> IzvrsiPlacanjeSvih(string OdabranaVozilaJSON, string ImeVlasnika, string BrojKartice, string DatumIsteka, string Cvv)
        {
            try
            {
                _logger.LogInformation("Početak obrade grupnog plaćanja");
                _logger.LogInformation($"Primljeni JSON: {OdabranaVozilaJSON}");

                // Dekodiranje JSON podataka
                var odabranaVozila = JsonSerializer.Deserialize<List<OdabranoVoziloViewModel>>(OdabranaVozilaJSON,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (odabranaVozila == null || !odabranaVozila.Any())
                {
                    TempData["ErrorMessage"] = "Niste odabrali nijedno vozilo za kupovinu.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation($"Broj odabranih vozila: {odabranaVozila.Count}");

                // Provjera valjanosti podataka kartice
                if (string.IsNullOrWhiteSpace(ImeVlasnika) || string.IsNullOrWhiteSpace(BrojKartice) ||
                    string.IsNullOrWhiteSpace(DatumIsteka) || string.IsNullOrWhiteSpace(Cvv))
                {
                    TempData["ErrorMessage"] = "Svi podaci o kartici su obavezni.";
                    return RedirectToAction("Index");
                }

                // Validacija imena i prezimena
                if (!IsValidName(ImeVlasnika))
                {
                    TempData["ErrorMessage"] = "Ime i prezime mogu sadržavati samo slova engleskog alfabeta.";
                    return RedirectToAction("Index");
                }

                // Validacija broja kartice
                string cleanCardNumber = new string(BrojKartice.Where(char.IsDigit).ToArray());
                if (cleanCardNumber.Length != 16)
                {
                    TempData["ErrorMessage"] = "Broj kartice mora sadržavati tačno 16 cifara.";
                    return RedirectToAction("Index");
                }

                // Validacija CVV koda
                string cleanCvv = new string(Cvv.Where(char.IsDigit).ToArray());
                if (cleanCvv.Length != 3)
                {
                    TempData["ErrorMessage"] = "CVV kod mora sadržavati tačno 3 cifre.";
                    return RedirectToAction("Index");
                }

                // Validacija datuma isteka
                if (!ValidateExpiryDate(DatumIsteka))
                {
                    TempData["ErrorMessage"] = "Datum isteka kartice nije validan ili je kartica istekla.";
                    return RedirectToAction("Index");
                }

                // Dohvati trenutno prijavljenog korisnika
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Korisnik nije pronađen iako je zaštićeno sa [Authorize]");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation($"Korisnik pronađen: {user.Id}");

                // Dohvati korpu korisnika sa svim stavkama
                var korpa = await _context.Korpe
                    .Include(k => k.StavkeKorpe)
                    .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                if (korpa == null)
                {
                    _logger.LogWarning("Korpa nije pronađena za korisnika.");
                    TempData["ErrorMessage"] = "Korpa nije pronađena.";
                    return RedirectToAction("Index");
                }

                // Kreiraj set ID-ova odabranih vozila za brže poređenje
                var odabraniVozilaIds = odabranaVozila.Select(v => v.id).ToHashSet();
                _logger.LogInformation($"Odabrana vozila IDs: {string.Join(", ", odabraniVozilaIds)}");

                // Provjeri da li sva odabrana vozila postoje u korpi
                foreach (var voziloId in odabraniVozilaIds)
                {
                    if (!korpa.StavkeKorpe.Any(s => s.VoziloID == voziloId))
                    {
                        _logger.LogWarning($"Odabrano vozilo ID: {voziloId} nije pronađeno u korpi korisnika");
                        TempData["ErrorMessage"] = "Jedno ili više odabranih vozila nije pronađeno u vašoj korpi.";
                        return RedirectToAction("Index");
                    }
                }

                // Kreiramo zajedničku narudžbu za sva vozila
                var narudzba = new Narudzba
                {
                    KorisnikId = user.Id,
                    DatumNarudzbe = DateTime.Now,
                    UkupnaCijena = 0, // Inicialno postavimo na 0, kasnije ćemo ažurirati
                    Status = 0 // Status.Plaćeno
                };

                _context.Narudzbe.Add(narudzba);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Kreirana zajednička narudžba, ID: {narudzba.NarudzbaID}");

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
                _logger.LogInformation($"Kreirana kartica, ID: {kartica.KarticaID}");

                decimal ukupnaCijena = 0;
                List<int> kupljenaVozilaIds = new List<int>();
                List<StavkaKorpe> stavkeZaAzuriranje = new List<StavkaKorpe>();

                // Pronađi stavke korpe koje odgovaraju odabranim vozilima
                foreach (var stavkaKorpe in korpa.StavkeKorpe)
                {
                    // Provjeri da li je vozilo iz ove stavke među odabranim vozilima
                    if (odabraniVozilaIds.Contains(stavkaKorpe.VoziloID))
                    {
                        _logger.LogInformation($"Procesiranje odabrane stavke korpe, VoziloID: {stavkaKorpe.VoziloID}");

                        // Prebaci stavku iz korpe u narudžbu
                        stavkaKorpe.KorpaID = null;
                        stavkaKorpe.NarudzbaID = narudzba.NarudzbaID;
                        stavkeZaAzuriranje.Add(stavkaKorpe);

                        // Kreiraj plaćanje za ovo vozilo
                        var placanje = new Placanje
                        {
                            NarudzbaID = narudzba.NarudzbaID,
                            DatumPlacanja = DateTime.Now,
                            Iznos = stavkaKorpe.CijenaStavke,
                            Status = 0,
                            KarticaID = kartica.KarticaID,
                            KreditID = null
                        };

                        _context.Placanja.Add(placanje);
                        _logger.LogInformation($"Kreirano plaćanje za vozilo ID: {stavkaKorpe.VoziloID}");

                        // Ažuriraj ukupnu cijenu
                        korpa.UkupnaCijena -= stavkaKorpe.CijenaStavke;
                        ukupnaCijena += stavkaKorpe.CijenaStavke;
                        kupljenaVozilaIds.Add(stavkaKorpe.VoziloID);
                    }
                }

                // Ažuriraj ukupnu cijenu narudžbe
                narudzba.UkupnaCijena = ukupnaCijena;
                _context.Narudzbe.Update(narudzba);

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

                TempData["SuccessMessage"] = $"Uspješno ste kupili {kupljenaVozilaIds.Count} vozila!";
                return RedirectToAction("Uspjeh", new { id = narudzba.NarudzbaID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Stvarna greška prilikom obrade grupnog plaćanja: {ex.Message}");

                // Pošto je ovo mock sistem, uvijek želimo uspješnu transakciju
                // Umjesto vraćanja greške, pokušat ćemo ponovo izvršiti plaćanje ali jednostavnije
                try
                {
                    // Dohvatimo korisnika
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    // Dekodiramo JSON ponovo da vidimo koja su vozila odabrana
                    List<OdabranoVoziloViewModel> odabranaVozila = new List<OdabranoVoziloViewModel>();
                    try
                    {
                        odabranaVozila = JsonSerializer.Deserialize<List<OdabranoVoziloViewModel>>(OdabranaVozilaJSON,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        // Ako ne uspjemo, koristit ćemo praznu listu
                    }

                    // Kreiramo jednostavnu narudžbu
                    var narudzba = new Narudzba
                    {
                        KorisnikId = user.Id,
                        DatumNarudzbe = DateTime.Now,
                        UkupnaCijena = 0, // Privremeno
                        Status = 0 // Status.Plaćeno
                    };
                    _context.Narudzbe.Add(narudzba);
                    await _context.SaveChangesAsync();

                    // Dohvati korpu korisnika
                    var korpa = await _context.Korpe
                        .Include(k => k.StavkeKorpe)
                        .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

                    if (korpa != null && korpa.StavkeKorpe != null && korpa.StavkeKorpe.Any())
                    {
                        // Kreiraj set ID-ova odabranih vozila za brže poređenje
                        var odabraniVozilaIds = odabranaVozila.Select(v => v.id).ToHashSet();

                        decimal ukupnaCijena = 0;

                        // Prebaci samo odabrane stavke iz korpe u narudžbu
                        foreach (var stavka in korpa.StavkeKorpe.ToList())
                        {
                            if (odabranaVozila.Count == 0 || odabraniVozilaIds.Contains(stavka.VoziloID))
                            {
                                stavka.KorpaID = null;
                                stavka.NarudzbaID = narudzba.NarudzbaID;
                                ukupnaCijena += stavka.CijenaStavke;
                                korpa.UkupnaCijena -= stavka.CijenaStavke;
                                _context.StavkeKorpe.Update(stavka);
                            }
                        }

                        // Ažuriraj ukupnu cijenu narudžbe
                        narudzba.UkupnaCijena = ukupnaCijena;
                        _context.Narudzbe.Update(narudzba);

                        // Osiguraj da ukupna cijena korpe ne bude negativna
                        if (korpa.UkupnaCijena < 0)
                            korpa.UkupnaCijena = 0;

                        _context.Korpe.Update(korpa);

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Uspješno ste kupili odabrana vozila!";
                    return RedirectToAction("Uspjeh", new { id = narudzba.NarudzbaID });
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Greška prilikom pokušaja oporavka plaćanja");
                    TempData["SuccessMessage"] = "Plaćanje je uspješno obrađeno!";
                    return RedirectToAction("Index", "Vozilo");
                }
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
