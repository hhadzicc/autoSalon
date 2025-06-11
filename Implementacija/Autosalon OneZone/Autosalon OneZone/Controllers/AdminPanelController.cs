using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autosalon_OneZone.Data; // Potrebno za ApplicationDbContext
using Autosalon_OneZone.Models; // Potrebno za Vozilo, TipGoriva, ApplicationUser itd.
using Autosalon_OneZone.ViewModels.Admin; // Potrebno za ViewModele
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// === DODANO: Using za Identity ===
using Microsoft.AspNetCore.Identity;
// ================================

namespace Autosalon_OneZone.Controllers
{
    [Authorize(Roles = "Administrator,Prodavac")]
    public class AdminPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // === DODANO: Privatna polja za UserManager i RoleManager ===
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        // ========================================================

        // === MODIFIKOVAN: Konstruktor sa UserManager i RoleManager ===
        public AdminPanelController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            // Dodani parametri u konstruktor
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            // Dodjela injected servisa privatnim poljima
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // ========================================================

        // Glavna stranica Admin Panela
        public IActionResult Index()
        {
            // Dobavljamo trenutnu sekciju iz query parametra, default je "Vozila"
            var currentSection = HttpContext.Request.Query["section"].ToString();
            if (string.IsNullOrEmpty(currentSection))
            {
                currentSection = "Vozila";
            }

            ViewBag.CurrentSection = currentSection;
            return View();
        }

        #region Vozila sekcija

        // Ajax metoda za učitavanje sekcije vozila
        // Ovu akciju sada koristimo za vraćanje cijelog partial Viewa sa inicijalnim podacima
        // (ako niste prešli na potpuni AJAX/JSON model za listu)
        // Ako ste prešli na AJAX/JSON model, ova akcija bi trebala vratiti partial View *bez* liste podataka
        // a podaci bi se dohvatali akcijom GetVozilaJson
        public async Task<IActionResult> GetVozilaSection(string searchQuery = null)
        {
            var query = _context.Vozila.AsQueryable();

            // Primjena filtera pretrage
            // Filter po Marki ILI Modelu, jer Naziv ne postoji
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(v => (v.Marka != null && v.Marka.Contains(searchQuery)) || (v.Model != null && v.Model.Contains(searchQuery)));
            }

            // Sortiranje i dobavljanje podataka
            var vozila = await query
        .OrderByDescending(v => v.VoziloID) // Najnovija vozila prva
                .ToListAsync();

            var viewModel = new VoziloListViewModel
            {
                Vozila = vozila, // Ako partial View i dalje koristi List<Vozilo> direktno
                SearchQuery = searchQuery
                // Dodajte paginaciju/sortiranje ako je primjenjivo na View model
            };

            return PartialView("_AdminVozila", viewModel);
        }

        // Akcija za dobavljanje forme za dodavanje vozila
        // Ovu akciju poziva JavaScript
        public IActionResult GetAddVoziloForm()
        {
            // Vraćamo prazan ViewModel za formu za dodavanje
            return PartialView("_AddVoziloForm", new AddVoziloViewModel());
        }

        // Akcija za dobavljanje forme za izmjenu vozila
        // Ovu akciju poziva JavaScript sa ID-om vozila
        public async Task<IActionResult> GetEditVoziloForm(int id)
        {
            var vozilo = await _context.Vozila.FindAsync(id);
            if (vozilo == null)
            {
                return NotFound(); // Vrati 404 ako vozilo nije pronađeno
            }

            // Popunjavamo EditVoziloViewModel podacima iz pronađenog Vozilo entiteta
            var viewModel = new EditVoziloViewModel
            {
                VoziloID = vozilo.VoziloID,
                // Mapiramo sa Marka i Model jer Naziv ne postoji
                Marka = vozilo.Marka,
                Model = vozilo.Model,
                Godiste = vozilo.Godiste,
                // Konvertujemo TipGoriva enum u string za ViewModel
                Gorivo = vozilo.Gorivo.ToString(),
                Kubikaza = vozilo.Kubikaza,
                Boja = vozilo.Boja,
                Kilometraza = vozilo.Kilometraza,
                Cijena = vozilo.Cijena,
                Opis = vozilo.Opis,
                PostojecaSlikaPath = vozilo.Slika, // Putanja do postojeće slike
                ZadrzatiPostojecuSliku = true // Default: zadrži postojeću sliku
            };

            // Vraćamo parcijalni View forme, koristeći EditViewModel (koji nasljeđuje AddViewModel)
            // Možete koristiti isti _AddVoziloForm.cshtml ako je dizajniran da radi sa oba View modela
            // ili imate poseban _EditVoziloForm.cshtml
            return PartialView("_AddVoziloForm", viewModel); // Ili "_EditVoziloForm"
        }

        // POST metoda za čuvanje (dodavanje ili izmjenu) vozila
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveVozilo(AddVoziloViewModel viewModel)
        {
            // Special validation for image - only required for new vehicles
            if (viewModel.VoziloID == 0 && viewModel.Slika == null)
            {
                ModelState.AddModelError("Slika", "Slika je obavezna za novo vozilo.");
            }

            // If this is an edit, remove any image validation errors
            if (viewModel.VoziloID > 0)
            {
                ModelState.Remove("Slika"); // Remove any validation errors for Slika field
            }

            // Check file type if an image was uploaded
            if (viewModel.Slika != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var extension = Path.GetExtension(viewModel.Slika.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Slika", "Dozvoljena su samo sljedeća proširenja fajlova: .jpg, .jpeg, .png, .gif, .bmp");
                }

                // Check file size (max 5MB)
                if (viewModel.Slika.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Slika", "Veličina slike ne smije prelaziti 5MB.");
                }
            }

            // ModelState validation
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

            // Handle image upload
            string uniqueFileName = null;

            if (viewModel.Slika != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.Slika.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                Directory.CreateDirectory(uploadsFolder); // Ensure directory exists

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.Slika.CopyToAsync(fileStream);
                }
            }

            // Get or create vehicle
            Vozilo vozilo;
            if (viewModel.VoziloID == 0) // New vehicle
            {
                vozilo = new Vozilo();
                _context.Vozila.Add(vozilo);
            }
            else // Existing vehicle
            {
                vozilo = await _context.Vozila.FindAsync(viewModel.VoziloID);
                if (vozilo == null)
                {
                    return NotFound();
                }
            }

            // Update vehicle properties
            vozilo.Marka = viewModel.Marka;
            vozilo.Model = viewModel.Model;
            vozilo.Godiste = viewModel.Godiste;

            try
            {
                vozilo.Gorivo = (TipGoriva)Enum.Parse(typeof(TipGoriva), viewModel.Gorivo);
            }
            catch (ArgumentException)
            {
                ModelState.AddModelError("Gorivo", "Odabrana vrijednost za gorivo nije validna.");
                return BadRequest(new
                {
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            vozilo.Kubikaza = viewModel.Kubikaza;
            vozilo.Boja = viewModel.Boja;
            vozilo.Kilometraza = viewModel.Kilometraza;
            vozilo.Cijena = viewModel.Cijena;
            vozilo.Opis = viewModel.Opis;

            // If a new image was uploaded, use it and delete the old one
            if (uniqueFileName != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(vozilo.Slika))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila", vozilo.Slika);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Set new image path
                vozilo.Slika = uniqueFileName;
            }
            // For edits, if no new image was uploaded, keep the existing one
            // No additional code needed - we're simply not updating vozilo.Slika

            await _context.SaveChangesAsync();

            return Ok(new
            {
                voziloId = vozilo.VoziloID,
                successMessage = viewModel.VoziloID > 0 ? "Vozilo uspješno izmijenjeno!" : "Vozilo uspješno dodano!"
            });
        }




        // POST metoda za brisanje vozila
        [HttpPost]
        // Promijenjen parametar u int id da odgovara AJAX pozivu
        public async Task<IActionResult> DeleteVozilo(int id)
        {
            var vozilo = await _context.Vozila.FindAsync(id);
            if (vozilo == null)
            {
                return NotFound(); // Vrati 404 ako vozilo nije pronađeno
            }

            // Brisanje slike sa servera ako postoji
            if (!string.IsNullOrEmpty(vozilo.Slika))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila", vozilo.Slika);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Vozila.Remove(vozilo); // Označi entitet za brisanje
            await _context.SaveChangesAsync(); // Sačuvaj promjene (izvrši brisanje)

            return Ok(new { successMessage = "Vozilo uspješno obrisano." }); // Vrati uspješan odgovor
        }

        #endregion

        #region Profili sekcija

        // AKCIJA SADA TREBA BITI async Task<IActionResult> JER DOHVATA PODATKE IZ BAZE
        public async Task<IActionResult> GetProfiliSection(string? searchQuery = null) // Dodan '?' na string
        {
            // Dohvati sve korisnike. KORISTITE _context.Users (iz IdentityDbContext)
            var usersQuery = _context.Users.AsQueryable();

            // Primjena filtera pretrage (po Korisničkom Imenu, Emailu, Imenu ili Prezmenu)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Pazite na null provjere za string svojstva!
                usersQuery = usersQuery.Where(u =>
          (u.UserName != null && u.UserName.Contains(searchQuery)) ||
          (u.Email != null && u.Email.Contains(searchQuery)) ||
          (u.Ime != null && u.Ime.Contains(searchQuery)) || // Pretpostavljamo da ApplicationUser ima Ime i Prezime
                    (u.Prezime != null && u.Prezime.Contains(searchQuery))
        );
            }

            // Ovdje možete dodati sortiranje (npr. po korisničkom imenu)
            // usersQuery = usersQuery.OrderBy(u => u.UserName);

            // Ovdje biste dodali i paginaciju (Skip/Take) ako radite paginaciju
            // var totalCount = await usersQuery.CountAsync();
            // var users = await usersQuery.Skip(...).Take(...).ToListAsync();

            var profili = await usersQuery.ToListAsync(); // <<<< DOHVATITE KORISNIKE IZ BAZE

            // Kreiramo ViewModel i popunjavamo listu korisnika
            var viewModel = new Autosalon_OneZone.ViewModels.Admin.ProfilListViewModel
            {
                Profili = profili, // <<<< PROSLEDITE DOHVAĆENU LISTU VIEW MODELU
                SearchQuery = searchQuery // Prosleđujemo query za inicijalizaciju input polja u Viewu
                // Dodajte TotalPages, CurrentPage ako implementirate paginaciju
            };

            // Vraćamo partial View sa popunjenim ViewModelom
            return PartialView("_AdminProfili", viewModel);
        }
        #endregion
        #region Recenzije sekcija

        // Promijenjeno iz async Task<IActionResult> u IActionResult jer nema await poziva
        // PROSLEDJUJEMO ViewModel
        public IActionResult GetRecenzijeSection(string? searchQuery = null)
        {
            // Kreiramo ViewModel. Ako _AdminRecenzije.cshtml očekuje listu recenzija u modelu,
            // trebali biste je dohvatiti ovde (kao u starom načinu) ili proslediti praznu listu
            // ako koristite AJAX/JSON za naknadno učitavanje liste.
            var viewModel = new Autosalon_OneZone.ViewModels.Admin.RecenzijaListViewModel
            {
                SearchQuery = searchQuery // Prosleđujemo trenutni query za inicijalizaciju polja za pretragu u Viewu
                                          // Recenzije = new List<Autosalon_OneZone.Models.Recenzija>() // Prosledite praznu listu ako View to očekuje
            };

            return PartialView("_AdminRecenzije", viewModel); // <<< PROSLEDITE ViewModel
        }
        #endregion
        #region Podrška sekcija

        // Promijenjeno iz async Task<IActionResult> u IActionResult jer nema await poziva
        // PROSLEDJUJEMO ViewModel
        public IActionResult GetPodrskaSection(string? searchQuery = null) // Dodan '?' na string
        {
            // Implementacija za prikaz liste upita podrške
            // Trebali biste dohvatiti podatke i proslijediti ViewModel partial Viewu
            // Za sada, kreiramo prazan ViewModel samo da View ne bude null

            var viewModel = new Autosalon_OneZone.ViewModels.Admin.PodrskaListViewModel // <-- Kreirajte instancu ViewModela
            {
                // Ako _AdminPodrska.cshtml treba inicijalne podatke, dohvatiti ih ovdje
                // Npr: PodrskaUpiti = await _context.PodrskaUpiti.ToListAsync(),
                SearchQuery = searchQuery // Proslijedite parametre ako View koristi ih
                                          // Ako View očekuje listu upita, osigurajte da svojstvo liste nije null:
                                          // Npr: PodrskaUpiti = new List<Autosalon_OneZone.Models.Podrska>()
            };


            return PartialView("_AdminPodrska", viewModel); // <<< PROSLEDITE ViewModel instancu
        }

        // Add this method to the AdminPanelController.cs file in the Podrška sekcija region
        [HttpGet]
        public async Task<JsonResult> GetPodrskaJson(string? searchQuery = null, int page = 1)
        {
            int pageSize = int.MaxValue;

            var query = _context.PodrskaUpiti
                .Include(p => p.Korisnik) // Include related user data
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    (p.Naslov != null && p.Naslov.Contains(searchQuery)) ||
                    (p.Sadrzaj != null && p.Sadrzaj.Contains(searchQuery)) ||
                    (p.Korisnik != null && p.Korisnik.Email.Contains(searchQuery))
                );
            }

            // Apply sorting - newest first
            query = query.OrderByDescending(p => p.DatumUpita);

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var upiti = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to JSON-friendly objects
            var jsonUpiti = upiti.Select(upit => new
            {
                upitID = upit.UpitID,
                datumUpita = upit.DatumUpita,
                korisnikId = upit.KorisnikId,
                korisnikEmail = upit.Korisnik?.Email ?? "N/A",
                korisnikIme = $"{upit.Korisnik?.Ime ?? ""} {upit.Korisnik?.Prezime ?? ""}".Trim(),
                naslov = upit.Naslov,
                sadrzaj = upit.Sadrzaj,
                status = upit.Status.ToString()
            }).ToList();

            // Return data and pagination info
            return Json(new
            {
                upiti = jsonUpiti,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }

        // Add this method to handle deletion of support requests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePodrska(int id)
        {
            var upit = await _context.PodrskaUpiti.FindAsync(id);
            if (upit == null)
            {
                return NotFound();
            }

            _context.PodrskaUpiti.Remove(upit);
            await _context.SaveChangesAsync();

            return Ok(new { successMessage = "Upit za podršku uspješno obrisan." });
        }

        // Add this method to handle changing the status of support requests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePodrskaStatus(int id, string status)
        {
            var upit = await _context.PodrskaUpiti.FindAsync(id);
            if (upit == null)
            {
                return NotFound();
            }

            // Try to parse the status string to the StatusUpita enum
            if (Enum.TryParse<StatusUpita>(status, out var statusEnum))
            {
                upit.Status = statusEnum;
                await _context.SaveChangesAsync();
                return Ok(new { successMessage = "Status upita uspješno promijenjen." });
            }
            else
            {
                return BadRequest("Nevažeći status.");
            }
        }

        #endregion


        // Add this method to the AdminPanelController.cs file in the Recenzije sekcija region
        [HttpGet]
        public async Task<JsonResult> GetRecenzijeJson(string? searchQuery = null, string? korisnikFilter = null, string? voziloFilter = null, int page = 1)
        {
            int pageSize = int.MaxValue;

            var query = _context.Recenzije
                .Include(r => r.Korisnik)
                .Include(r => r.Vozilo)
                .AsQueryable();

            // Apply user filter
            if (!string.IsNullOrEmpty(korisnikFilter))
            {
                query = query.Where(r =>
                    (r.Korisnik.UserName != null && r.Korisnik.UserName.Contains(korisnikFilter)) ||
                    (r.Korisnik.Email != null && r.Korisnik.Email.Contains(korisnikFilter)) ||
                    (r.Korisnik.Ime != null && r.Korisnik.Ime.Contains(korisnikFilter)) ||
                    (r.Korisnik.Prezime != null && r.Korisnik.Prezime.Contains(korisnikFilter))
                );
            }

            // Apply vehicle filter
            if (!string.IsNullOrEmpty(voziloFilter))
            {
                query = query.Where(r =>
                    (r.Vozilo.Marka != null && r.Vozilo.Marka.Contains(voziloFilter)) ||
                    (r.Vozilo.Model != null && r.Vozilo.Model.Contains(voziloFilter))
                );
            }

            // Apply text search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(r => r.Komentar != null && r.Komentar.Contains(searchQuery));
            }

            // Sort by date - newest first
            query = query.OrderByDescending(r => r.DatumRecenzije);

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var recenzije = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to JSON-friendly objects
            var jsonRecenzije = recenzije.Select(r => new
            {
                recenzijaID = r.RecenzijaID,
                korisnikId = r.KorisnikId,
                korisnikUserName = r.Korisnik?.UserName ?? "N/A",
                korisnikIme = $"{r.Korisnik?.Ime ?? ""} {r.Korisnik?.Prezime ?? ""}".Trim(),
                voziloID = r.VoziloID,
                voziloMarka = r.Vozilo?.Marka ?? "N/A",
                voziloModel = r.Vozilo?.Model ?? "",
                voziloNaziv = $"{r.Vozilo?.Marka ?? ""} {r.Vozilo?.Model ?? ""}".Trim(),
                ocjena = r.Ocjena,
                komentar = r.Komentar,
                datumRecenzije = r.DatumRecenzije
            }).ToList();

            // Return data and pagination info
            return Json(new
            {
                recenzije = jsonRecenzije,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }

        // Add this method to handle deletion of reviews
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecenzija(int id)
        {
            var recenzija = await _context.Recenzije.FindAsync(id);
            if (recenzija == null)
            {
                return NotFound();
            }

            _context.Recenzije.Remove(recenzija);
            await _context.SaveChangesAsync();

            return Ok(new { successMessage = "Recenzija uspješno obrisana." });
        }

        // Unutar AdminPanelController.cs klase

        // Dodajte ovu novu akciju
        [HttpGet] // Označava da je ova akcija namijenjena za GET zahtjeve (što AJAX $.get i jeste)
        public async Task<JsonResult> GetVozilaJson(string? searchQuery = null, int page = 1, string? sortOrder = null) // Dodani parametri
        {
            // Change this line:
            // int pageSize = 10;

            // To this (a very large number to effectively show all):
            int pageSize = int.MaxValue;

            var query = _context.Vozila.AsQueryable();

            // Primjena filtera pretrage po Marki ili Modelu (isto kao u GetVozilaSection)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(v => (v.Marka != null && v.Marka.Contains(searchQuery)) || (v.Model != null && v.Model.Contains(searchQuery)));
            }

            // Primjena sortiranja (prilagodite prema svojim potrebama i sortOrder vrijednostima)
            // Pazite na null vrijednosti kod sortiranja nullable tipova ako ih ima
            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(v => v.Cijena);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(v => v.Cijena);
                    break;
                case "year_asc":
                    query = query.OrderBy(v => v.Godiste); // Pazite ako je Godiste nullable
                    break;
                case "year_desc":
                    query = query.OrderByDescending(v => v.Godiste); // Pazite ako je Godiste nullable
                    break;
                // Dodajte ostale opcije sortiranja (npr. po Marki, Modelu...)
                default: // Podrazumijevano sortiranje (npr. po ID-u)
                    query = query.OrderByDescending(v => v.VoziloID);
                    break;
            }

            // Dobijanje ukupnog broja stavki za paginaciju
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize); // Izračunajte ukupan broj stranica

            // Primjena paginacije (preskoči X stavki, uzmi Y stavki)
            var vozila = await query
        .Skip((page - 1) * pageSize) // Preskoči stavke sa prethodnih stranica
                .Take(pageSize) // Uzmi stavke za trenutnu stranicu
                .ToListAsync();

            // Mapiranje Vozilo entiteta u objekte pogodne za JSON serijalizaciju
            // Ovo je KLJUČNO jer JSON objekti moraju imati svojstva sa imenima (npr. 'naziv', 'cijena')
            // kako ih očekuje JavaScript u _AdminVozila.cshtml.
            var jsonVozila = vozila.Select(v => new
            {
                voziloID = v.VoziloID, // Naziv mora biti isti kao u JS (camelCase po defaultu)
                                       // JS očekuje 'naziv', ali entitet ima 'Marka' i 'Model'. Spojićemo ih.
                naziv = $"{v.Marka} {v.Model}".Trim(), // Kreiramo 'naziv' za JS
                godiste = v.Godiste,
                gorivo = v.Gorivo.ToString(), // Konvertujemo enum u string za JSON
                kilometraza = v.Kilometraza,
                cijena = v.Cijena,
                boja = v.Boja, // Dodajte ostala svojstva koja vam trebaju u prikazu liste
                kubikaza = v.Kubikaza,
                // Slika i Opis možda nisu potrebni u tabeli, ali ako jesu, dodajte ih
                // slika = v.Slika,
                // opis = v.Opis
            }).ToList();

            // Vraćanje podataka i informacija o paginaciji kao JSON
            return Json(new
            {
                vozila = jsonVozila, // Lista objekata
                totalCount = totalCount, // Ukupan broj stavki
                totalPages = totalPages, // Ukupan broj stranica
                currentPage = page, // Trenutna stranica
                pageSize = pageSize // Veličina stranice
            });
        }

        // ... ostale akcije ...





        // 1. Add this method to get JSON data for user list
        [HttpGet]
        public async Task<JsonResult> GetProfiliJson(string? searchQuery = null, int page = 1)
        {
            // Change this line:
            // int pageSize = 10;

            // To this:
            int pageSize = int.MaxValue;

            var query = _context.Users.AsQueryable();

            // Primjena filtera pretrage
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchQuery)) ||
                    (u.Email != null && u.Email.Contains(searchQuery)) ||
                    (u.Ime != null && u.Ime.Contains(searchQuery)) ||
                    (u.Prezime != null && u.Prezime.Contains(searchQuery))
                );
            }

            // Primjena sortiranja
            query = query.OrderBy(u => u.UserName);

            // Dobijanje ukupnog broja stavki za paginaciju
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Primjena paginacije
            var profili = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lista za JSON rezultat
            var jsonProfili = new List<object>();

            // Za svakog korisnika dohvati uloge
            foreach (var user in profili)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string uloga = string.Join(", ", roles); // Spoji više uloga ako ih ima

                jsonProfili.Add(new
                {
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    ime = user.Ime,
                    prezime = user.Prezime,
                    uloga = uloga // Dodajemo ulogu za prikaz u tabeli
                });
            }

            return Json(new
            {
                profili = jsonProfili,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }


        // 2. Add this method to get the form for adding a new user
        public async Task<IActionResult> GetAddProfilForm()
        {
            var viewModel = new AddProfilViewModel();

            // Ako koristite role, dohvatite ih i dodajte u ViewModel
            var roles = await _roleManager.Roles.ToListAsync();
            viewModel.DostupneRole = roles;

            return PartialView("_AddProfilForm", viewModel);
        }

        // 3. Add this method to get the form for editing an existing user
        public async Task<IActionResult> GetEditProfilForm(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Popunjavamo ViewModel podacima korisnika
            var viewModel = new AddProfilViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Ime = user.Ime,
                Prezime = user.Prezime
            };

            // Dohvatanje i postavljanje uloga korisnika
            var roles = await _roleManager.Roles.ToListAsync();
            viewModel.DostupneRole = roles;
            viewModel.OdabraneRole = (await _userManager.GetRolesAsync(user)).ToList();

            return PartialView("_AddProfilForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfil(AddProfilViewModel viewModel)
        {
            // Ako je ovo uređivanje (ima UserId) a ne dodavanje, lozinka nije obavezna
            if (!string.IsNullOrEmpty(viewModel.UserId))
            {
                // Ukloni validaciju za lozinku ako je edit mode
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            // Pretvori OdabraneRole iz string u List<string> ako je to potrebno
            // Ovo rješava problem kad se koristi radio button umjesto checkbox
            if (viewModel.OdabraneRole != null && viewModel.OdabraneRole.Count == 0 && Request.Form["OdabraneRole"].Count > 0)
            {
                viewModel.OdabraneRole = new List<string> { Request.Form["OdabraneRole"].ToString() };
            }

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

            ApplicationUser user;
            var identityErrors = new List<string>();

            // Ako je UserId prazan, radi se o novom korisniku
            if (string.IsNullOrEmpty(viewModel.UserId))
            {
                user = new ApplicationUser
                {
                    UserName = viewModel.UserName,
                    Email = viewModel.Email,
                    Ime = viewModel.Ime ?? string.Empty,
                    Prezime = viewModel.Prezime ?? string.Empty
                };

                var result = await _userManager.CreateAsync(user, viewModel.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        identityErrors.Add(error.Description);
                    }
                    return BadRequest(new
                    {
                        identityErrors = identityErrors
                    });
                }
            }
            else // Ažuriranje postojećeg korisnika
            {
                user = await _userManager.FindByIdAsync(viewModel.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                // Provjeri da li korisničko ime već postoji za nekoga drugog
                if (user.UserName != viewModel.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(viewModel.UserName);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest(new
                        {
                            identityErrors = new[] { "Korisnik s tim korisničkim imenom već postoji." }
                        });
                    }
                }

                // Provjeri da li email već postoji za nekoga drugog
                if (user.Email != viewModel.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest(new
                        {
                            identityErrors = new[] { "Korisnik s tim email-om već postoji." }
                        });
                    }
                }

                user.UserName = viewModel.UserName;
                user.Email = viewModel.Email;
                user.Ime = viewModel.Ime ?? string.Empty;
                user.Prezime = viewModel.Prezime ?? string.Empty;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        identityErrors.Add(error.Description);
                    }
                    return BadRequest(new
                    {
                        identityErrors = identityErrors
                    });
                }

                // Promjena lozinke ako je navedena
                if (!string.IsNullOrEmpty(viewModel.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, viewModel.Password);

                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            identityErrors.Add(error.Description);
                        }
                        return BadRequest(new
                        {
                            identityErrors = identityErrors
                        });
                    }
                }
            }

            // Ažuriranje uloga korisnika
            if (viewModel.OdabraneRole != null && viewModel.OdabraneRole.Any())
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, viewModel.OdabraneRole);
            }
            else
            {
                // Ako nije odabrana niti jedna uloga, dodajemo podrazumijevanu ulogu "Kupac"
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Any())
                {
                    await _userManager.AddToRoleAsync(user, "Kupac");
                }
            }

            return Ok(new { userId = user.Id, successMessage = "Korisnik uspješno sačuvan!" });
        }



        // 5. Add this method to delete a user
        [HttpPost]
        public async Task<IActionResult> DeleteProfil(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // 1. Dohvati sve narudžbe korisnika
                var userOrders = await _context.Narudzbe
                    .Where(n => n.KorisnikId == id)
                    .ToListAsync();

                // 2. Za svaku narudžbu pronađi plaćanje i stavke korpe
                foreach (var order in userOrders)
                {
                    // Prvo obriši plaćanje povezano s narudžbom (ako postoji)
                    var payment = await _context.Placanja
                        .FirstOrDefaultAsync(p => p.NarudzbaID == order.NarudzbaID);

                    if (payment != null)
                    {
                        _context.Placanja.Remove(payment);
                    }

                    // Zatim obriši stavke korpe povezane s narudžbom
                    var orderItems = await _context.StavkeKorpe
                        .Where(s => s.NarudzbaID == order.NarudzbaID)
                        .ToListAsync();

                    if (orderItems.Any())
                    {
                        _context.StavkeKorpe.RemoveRange(orderItems);
                    }
                }

                // 3. Sačuvaj promjene prije brisanja korisnika
                await _context.SaveChangesAsync();

                // 4. Sada briši korisnika
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors.Select(e => e.Description));
                }

                return Ok(new { successMessage = "Korisnik uspješno obrisan." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errorMessage = $"Greška prilikom brisanja korisnika: {ex.Message}" });
            }
        }


    }
}