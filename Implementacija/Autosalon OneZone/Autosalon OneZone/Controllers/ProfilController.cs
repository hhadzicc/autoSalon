// Put this file in your Controllers folder, e.g., Autosalon_OneZone/Controllers/ProfilController.cs

// Potrebni using directives za ovaj kontroler
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Autosalon_OneZone.Models; // Namespace gde je ApplicationUser (i vasi drugi modeli ako ih koristite)
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Za [Authorize] atribut
using Autosalon_OneZone.Models.ViewModels; // Namespace gde su vasi ViewModel-i (ProfileViewModel, EditProfileViewModel)
using Microsoft.Extensions.Logging; // Za logovanje (opciono)
using System.Linq; // Potrebno za FirstOrDefault() na listi rola
using System.Collections.Generic; // Potrebno za List<T> (za recenzije)

// *** AKO KORISTITE Entity Framework Core za pristup Bazi i dohvatanje recenzija, DODAJTE SLEDEĆE USINGE: ***
// using Autosalon_OneZone.Data; // Primer: Namespace gde Vam je ApplicationDbContext
// using Microsoft.EntityFrameworkCore; // Primer: Za metode poput .Include(), .Where(), .Select(), .ToListAsync()
// *******************************************************************************************************


namespace Autosalon_OneZone.Controllers
{
    // Kontroler za upravljanje korisničkim profilima
    // Atribut [Authorize] na nivou klase znači da sve akcije u ovom kontroleru zahtevaju da korisnik bude prijavljen
    [Authorize]
    public class ProfilController : Controller
    {
        // Privatna polja za injektovane servise
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // Često koristan i u profil akcijama (npr. RefreshSignInAsync)
        private readonly ILogger<ProfilController> _logger; // Logger specifičan za Profil kontroler

        // *** AKO KORISTITE DbContext za dohvatanje drugih podataka (npr. recenzija), DODAJTE READONLY POLJE: ***
        // private readonly ApplicationDbContext _context; // Primer
        // ***************************************************************************************************
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
                return NotFound("Korisnik nije pronađen.");
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
            _logger?.LogInformation("User changed their password successfully.");

            return Ok(new { message = "Lozinka uspješno promijenjena." });
        }


        // Konstruktor za injektovanje potrebnih servisa
        // U listi parametara dodajte sve servise koje kontroler koristi (poput DbContext-a ako vam treba)
        public ProfilController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ProfilController> logger
            // *** AKO KORISTITE DbContext, DODAJTE GA U PARAMETRE KONSTRUKTORA: ***
            // , ApplicationDbContext context // Primer
            // ******************************************************************
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            // *** AKO KORISTITE DbContext, DODELITE GA U KONSTRUKTORU: ***
            // _context = context; // Primer
            // **********************************************************
        }


        // --- AKCIJE ZA UPRAVLJANJE PROFILOM KORISNIKA ---
        // View fajlovi za ove akcije se nalaze u folderu Views/Profil/

        // GET: /Profil/Index
        // Prikazuje profil prijavljenog korisnika
        // Često se koristi Index kao podrazumevana akcija kontrolera, pa je View fajl Views/Profil/Index.cshtml
        [HttpGet]
        public async Task<IActionResult> Index() // Akciju smo nazvali Index, odgovara View-u Views/Profil/Index.cshtml
        {
            // Dohvati trenutno prijavljenog korisnika iz Identity sistema na osnovu User principala
            var user = await _userManager.GetUserAsync(User);

            // Provera da li je korisnik pronadjen (Authorize atribut osigurava da je prijavljen, ali provera je dobra praksa)
            if (user == null)
            {
                _logger?.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen prilikom ucitavanja profila.");
                // Ako korisnik ne može biti pronađen, možda ga treba odjaviti
                // await _signInManager.SignOutAsync();
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen.");
            }

            // --- Dohvatanje Role Korisnika ---
            // Dohvati sve role koje korisnik ima iz Identity sistema
            var roles = await _userManager.GetRolesAsync(user);
            // Za jednostavan prikaz u ViewModelu, uzimamo prvu rolu ili postavljamo podrazumevanu ("Klijent")
            string role = roles.FirstOrDefault() ?? "Klijent"; // Primer: Uzimamo prvu rolu ili default vrednost

            // --- Dohvatanje Recenzija Korisnika ---
            // *** OVDE MORATE DODATI VAŠU LOGIKU ZA DOHVATANJE RECENZIJA IZ BAZE PODATAKA ***
            // Ovo zahteva korišćenje Vašeg DbContext-a i upita nad tabelom Recenzija (npr. _context.Recenzije).
            // Filtrirajte po KorisnikId-u (ako je to svojstvo u Vašem modelu Recenzija) i mapirajte na ProfileViewModel.ReviewViewModel.
            // Primer (ZAMENITE SA VAŠOM IMPLEMENTACIJOM KORISTEĆI VAŠ DbContext):
            /*
            var userReviews = await _context.Recenzije // Pristup DbContext-u i tabeli Recenzija
                .Include(r => r.Vozilo) // Ukljuci povezane podatke o Vozilu ako prikazujes VoziloNaziv
                .Where(r => r.KorisnikId == user.Id) // Filtriraj recenzije po ID-u korisnika (pretpostavka)
                .Select(r => new ProfileViewModel.ReviewViewModel { // Mapiraj podatke iz baze na ViewModel
                    VoziloNaziv = r.Vozilo.Naziv, // Pretpostavka: Recenzija ima navigaciono svojstvo Vozilo sa Nazivom
                    Ocena = r.Ocena,
                    Tekst = r.Tekst
                    // Dodajte ostale podatke iz Recenzije modela potrebne za ReviewViewModel
                })
                .ToListAsync(); // Izvrši upit asinhrono i vrati listu
            */
            // *** ZAMENITE SLEDEĆI KOD SA VAŠOM STVARNOM LOGIKOM ZA DOHVATANJE RECENZIJA IZ BAZE ***
            // Ako još niste implementirali dohvatanje iz baze, ostavite praznu listu ili dummy podatke za test
            var userReviews = new List<ProfileViewModel.ReviewViewModel>(); // Prazna lista kao placeholder

            // Primer dummy podataka (ako zelite da testirate prikaz pre povezivanja na bazu):
            /*
            userReviews.Add(new ProfileViewModel.ReviewViewModel { VoziloNaziv = "Dummy Auto 1", Ocena = 5, Tekst = "Odlicno vozilo, prezadovoljan sam!" });
            userReviews.Add(new ProfileViewModel.ReviewViewModel { VoziloNaziv = "Dummy Auto 2", Ocena = 4, Tekst = "Vrlo dobar auto, preporucujem." });
            */
            // ***************************************************************************


            // Kreiraj instancu ProfileViewModel-a i popuni je dohvaćenim podacima
            var model = new ProfileViewModel
            {
                ImePrezime = $"{user.Ime} {user.Prezime}", // Kombinuje Ime i Prezime iz ApplicationUser
                Email = user.Email, // Email iz ApplicationUser
                UserName = user.UserName, // Korisničko ime iz ApplicationUser (često isto kao Email)
                Role = role, // Dodaje rolu(e)
                Recenzije = userReviews // Dodaje listu recenzija (trenutno praznu bez implementacije iz baze)
            };

            // Vrati View Views/Profil/Index.cshtml i prosledi mu kreirani model
            // MVC View Engine će automatski tražiti Views/Profil/Index.cshtml za Index akciju u ProfilControlleru
            // Ako ste View fajl nazvali Profile.cshtml, promenite return View("Profile", model);
            return View(model);
        }


        // GET: /Profil/Edit
        // Prikazuje formu za izmenu profila prijavljenog korisnika
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Dohvati trenutno prijavljenog korisnika
            var user = await _userManager.GetUserAsync(User);

            // Provera da li je korisnik pronadjen
            if (user == null)
            {
                _logger?.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen prilikom ucitavanja forme za izmenu profila.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen.");
            }

            // Kreiraj ViewModel sa podacima za formu za izmenu profila
            // *** MORATE KREIRATI EditProfileViewModel klasu u folderu Models/ViewModels ***
            var model = new EditProfileViewModel // Kreirajte ovu klasu sa potrebnim svojstvima
            {
                Ime = user.Ime, // Popuni postojecim imenom iz ApplicationUser
                Prezime = user.Prezime, // Popuni postojecim prezimenom iz ApplicationUser
                Email = user.Email, // Popuni postojecim emailom iz ApplicationUser
                UserName = user.UserName // Popuni postojecim korisnickim imenom iz ApplicationUser
                // Polja za lozinku (OldPassword, NewPassword, ConfirmNewPassword) se NE popunjavaju iz sigurnosnih razloga
            };

            // Vrati View Views/Profil/Edit.cshtml i prosledi mu model popunjen trenutnim podacima
            // MVC View Engine ce automatski traziti Views/Profil/Edit.cshtml za Edit akciju u ProfilControlleru
            return View(model);
        }


        // POST: /Profil/Edit
        // Procesira submit forme za izmenu profila
        [HttpPost]
        [ValidateAntiForgeryToken] // Zaštita od CSRF napada - neophodno!
        public async Task<IActionResult> Edit(EditProfileViewModel model) // Prihvata ViewModel popunjen podacima iz forme za izmenu
        {
            // Dohvati trenutno prijavljenog korisnika iz baze na osnovu User principala
            var user = await _userManager.GetUserAsync(User);

            // Provera da li je korisnik pronadjen
            if (user == null)
            {
                _logger?.LogError($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen prilikom obrade submitovane forme za izmenu profila.");
                return NotFound($"Korisnik sa ID-om '{_userManager.GetUserId(User)}' nije pronadjen.");
            }

            // Proveri validaciju podataka koje je korisnik uneo u formu (na osnovu Data Annotations u EditProfileViewModel)
            if (!ModelState.IsValid)
            {
                // Ako podaci nisu validni, vrati View Views/Profil/Edit.cshtml sa unesenim podacima i greskama
                // Greške će se prikazati u asp-validation-summary i asp-validation-for u Viewu
                return View(model);
            }

            // --- LOGIKA AŽURIRANJA PODATAKA KORISNIKA ---
            // Ova logika proverava da li su se podaci promenili i ažurira ApplicationUser objekat.
            // Koristimo flag da pratimo da li je bilo promena na profilu (osim lozinke) koje zahtevaju _userManager.UpdateAsync()
            bool profileChanged = false;

            // Ažuriranje Imena ako se razlikuje od postojećeg
            if (user.Ime != model.Ime)
            {
                user.Ime = model.Ime;
                profileChanged = true;
            }

            // Ažuriranje Prezimena ako se razlikuje od postojećeg
            if (user.Prezime != model.Prezime)
            {
                user.Prezime = model.Prezime;
                profileChanged = true;
            }

            // *** LOGIKA ZA AŽURIRANJE EMAILA - SLOŽENIJE, ČESTO ZAHTEVA POTVRDU EMAILA ***
            // Ako menjate email, obično šaljete token na novi email i tražite korisniku da ga potvrdi.
            // Primer (nekompletan, zahteva implementaciju email servisa i procesa potvrde):

            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri postavljanju novog Emaila.");
                    // Dodajte sve greške iz setEmailResult.Errors u ModelState
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model); // Vrati View sa greškom
                }
                // Ovdje bi trebalo generisati token za potvrdu emaila i poslati email korisniku
                profileChanged = true; // Smatramo promenu emaila kao promenu profila
            }


            // *** LOGIKA ZA AŽURIRANJE KORISNIČKOG IMENA - PAZITI NA JEDINSTVENOST ***
            // Ako dozvoljavate izmenu Korisničkog imena, mora biti jedinstveno.
            // Primer:

            if (model.UserName != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri postavljanju novog korisničkog imena.");
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model); // Vrati View sa greškom
                }
                profileChanged = true; // Označavamo da je profil promenjen
            }


            // --- LOGIKA ZA PROMENU LOZINKE ---
            // *** OVO JE KRITIČNO ZA SIGURNOST I OBICNO SE RADI NA ZASEBNOJ STRANICI (ChangePassword) ***
            // Ako želite promenu lozinke ovde, Vaš EditProfileViewModel MORA imati polja OldPassword, NewPassword, ConfirmNewPassword.
            // I MORATE KORISTITI UserManager.ChangePasswordAsync(user, oldPassword, newPassword) METODU KOJA PROVERAVA STARU LOZINKU.
            // ZAMENITE SLEDEĆI KOD SA ISPRAVNOM IMPLEMENTACIJOM AKO ŽELITE PROMENU LOZINKE OVDE!
            /*
            if (!string.IsNullOrWhiteSpace(model.NewPassword)) // Provera da li je korisnik uneo novu lozinku
            {
                // *** VAŽNO: OVDE MORATE PROVERITI STARU LOZINKU PRE NEGO ŠTO JE PROMENITE ***
                // Koristite _userManager.CheckPasswordAsync(user, model.OldPassword)
                // A ZATIM _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword)

                 // Primer ispravnog (ali nekompletnog bez provere OldPassword u ViewModelu) koda:
                 // var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                 // if (!changePasswordResult.Succeeded)
                 // {
                 //      ModelState.AddModelError(string.Empty, "Greška pri promeni lozinke. Proverite staru lozinku i novu lozinku.");
                 //      foreach (var error in changePasswordResult.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
                 //      return View(model); // Vrati View sa greškom
                 // }
                // --------------------------------------------------------------------------------

                // Ako niste implementirali sigurnu promenu lozinke sa proverom stare lozinke:
                ModelState.AddModelError(string.Empty, "Implementacija promene lozinke zahteva proveru stare lozinke!"); // Placeholder greška
                 // Morate vratiti View sa greškom ako promena lozinke nije uspela ili nije pravilno implementirana
                 return View(model);
            }
            */
            // --- KRAJ LOGIKE ZA PROMENU LOZINKE ---


            // --- AŽURIRAJ KORISNIKA U BAZI AKO JE BILO PROMENA NA PROFILU (osim lozinke) ---
            if (profileChanged)
            {
                var updateProfileResult = await _userManager.UpdateAsync(user);
                if (!updateProfileResult.Succeeded)
                {
                    // Ako ažuriranje profila nije uspelo, dodaj greške
                    foreach (var error in updateProfileResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model); // Vrati View sa greškama
                }
                // Osveži sign-in cookie da odrazi promene na profilu (npr. Ime/Prezime)
                await _signInManager.RefreshSignInAsync(user);
            }


            // --- ZAVRŠETAK AKCIJE ---
            // Ako je ažuriranje profila (ako ga je bilo) I eventualna promena lozinke (ako je bila pokušana i uspešna) završeno bez grešaka
            // Postavi poruku o uspehu i preusmeri na stranicu profila

            // Postavi poruku o uspehu samo ako je bilo promena profila ILI ako je promena lozinke bila uspešna
            if (profileChanged /* || (ako je promena lozinke bila uspesna) */)
            {
                _logger?.LogInformation("User profile updated successfully.");
                // --- Postavi poruku o uspehu u TempData pre preusmeravanja ---
                // Ova poruka ce se prikazati na sledecoj stranici (Index/Profile) ako je implementiran prikaz TempData u Layoutu ili Viewu
                //TempData["SuccessMessage"] = "Profil uspešno ažuriran.";
                // -----------------------------------------------------------
            }
            else
            {
                // Ako nije bilo promena profila niti pokusaja promene lozinke, mozda informativna poruka
                //TempData["InfoMessage"] = "Nema promena na profilu.";
            }


            // Preusmeri na stranicu profila (Index akciju ovog kontrolera) nakon uspesne izmene ili ako nije bilo promena
            return RedirectToAction("Index", "Profil"); // Preusmeri nazad na GET Index akciju u ProfilControlleru

            // Ako je došlo do grešaka (ModelState.IsValid je bio false, UserManager operacija nije uspela, itd.)
            // kod je već vratio View(model) ranije u metodi.

        }
        // --- KRAJ AKCIJA ZA UPRAVLJANJE PROFILOM KORISNIKA ---

    }
}