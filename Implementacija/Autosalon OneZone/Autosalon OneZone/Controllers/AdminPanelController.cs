using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.Controllers
{
    [Authorize(Roles = "Administrator,Prodavac")]
    public class AdminPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminPanelController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;

            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var currentSection = HttpContext.Request.Query["section"].ToString();
            if (string.IsNullOrEmpty(currentSection))
            {
                currentSection = "Vozila";
            }

            ViewBag.CurrentSection = currentSection;
            return View();
        }

        #region Vozila sekcija

        public async Task<IActionResult> GetVozilaSection(string searchQuery = null)
        {
            var query = _context.Vozila.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(v => (v.Marka != null && v.Marka.Contains(searchQuery)) || (v.Model != null && v.Model.Contains(searchQuery)));
            }

            var vozila = await query
              .OrderByDescending(v => v.VoziloID)
              .ToListAsync();

            var viewModel = new VoziloListViewModel
            {
                Vozila = vozila,
                SearchQuery = searchQuery
            };

            return PartialView("_AdminVozila", viewModel);
        }

        public IActionResult GetAddVoziloForm()
        {
            return PartialView("_AddVoziloForm", new AddVoziloViewModel());
        }

        public async Task<IActionResult> GetEditVoziloForm(int id)
        {
            var vozilo = await _context.Vozila.FindAsync(id);
            if (vozilo == null)
            {
                return NotFound();
            }

            var viewModel = new EditVoziloViewModel
            {
                VoziloID = vozilo.VoziloID,
                Marka = vozilo.Marka,
                Model = vozilo.Model,
                Godiste = vozilo.Godiste,
                Gorivo = vozilo.Gorivo.ToString(),
                Kubikaza = vozilo.Kubikaza,
                Boja = vozilo.Boja,
                Kilometraza = vozilo.Kilometraza,
                Cijena = vozilo.Cijena,
                Opis = vozilo.Opis,
                PostojecaSlikaPath = vozilo.Slika,
                ZadrzatiPostojecuSliku = true
            };

            return PartialView("_AddVoziloForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveVozilo(AddVoziloViewModel viewModel)
        {
            if (viewModel.VoziloID == 0 && viewModel.Slika == null)
            {
                ModelState.AddModelError("Slika", "Slika je obavezna za novo vozilo.");
            }

            if (viewModel.VoziloID > 0)
            {
                ModelState.Remove("Slika");
            }

            if (viewModel.Slika != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var extension = Path.GetExtension(viewModel.Slika.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Slika", "Dozvoljena su samo sljedeća proširenja fajlova: .jpg, .jpeg, .png, .gif, .bmp");
                }

                if (viewModel.Slika.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Slika", "Veličina slike ne smije prelaziti 5MB.");
                }
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

            string uniqueFileName = null;

            if (viewModel.Slika != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.Slika.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                Directory.CreateDirectory(uploadsFolder);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.Slika.CopyToAsync(fileStream);
                }
            }

            Vozilo vozilo;
            if (viewModel.VoziloID == 0)
            {
                vozilo = new Vozilo();
                _context.Vozila.Add(vozilo);
            }
            else
            {
                vozilo = await _context.Vozila.FindAsync(viewModel.VoziloID);
                if (vozilo == null)
                {
                    return NotFound();
                }
            }

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

            if (uniqueFileName != null)
            {
                if (!string.IsNullOrEmpty(vozilo.Slika))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila", vozilo.Slika);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                vozilo.Slika = uniqueFileName;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                voziloId = vozilo.VoziloID,
                successMessage = viewModel.VoziloID > 0 ? "Vozilo uspješno izmijenjeno!" : "Vozilo uspješno dodano!"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVozilo(int id)
        {
            var vozilo = await _context.Vozila.FindAsync(id);
            if (vozilo == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(vozilo.Slika))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/vozila", vozilo.Slika);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Vozila.Remove(vozilo);
            await _context.SaveChangesAsync();

            return Ok(new { successMessage = "Vozilo uspješno obrisano." });
        }

        #endregion

        #region Profili sekcija

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetProfiliSection(string? searchQuery = null)
        {
            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u =>
                  (u.UserName != null && u.UserName.Contains(searchQuery)) ||
                  (u.Email != null && u.Email.Contains(searchQuery)) ||
                  (u.Ime != null && u.Ime.Contains(searchQuery)) ||
                  (u.Prezime != null && u.Prezime.Contains(searchQuery))
                );
            }

            var profili = await usersQuery.ToListAsync();

            var viewModel = new Autosalon_OneZone.ViewModels.Admin.ProfilListViewModel
            {
                Profili = profili,
                SearchQuery = searchQuery
            };

            return PartialView("_AdminProfili", viewModel);
        }
        #endregion
        #region Recenzije sekcija

        public IActionResult GetRecenzijeSection(string? searchQuery = null)
        {
            var viewModel = new Autosalon_OneZone.ViewModels.Admin.RecenzijaListViewModel
            {
                SearchQuery = searchQuery
            };

            return PartialView("_AdminRecenzije", viewModel);
        }
        #endregion
        #region Podrška sekcija

        public IActionResult GetPodrskaSection(string? searchQuery = null)
        {
            var viewModel = new Autosalon_OneZone.ViewModels.Admin.PodrskaListViewModel
            {
                SearchQuery = searchQuery
            };

            return PartialView("_AdminPodrska", viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetPodrskaJson(string? searchQuery = null, int page = 1)
        {
            int pageSize = int.MaxValue;

            var query = _context.PodrskaUpiti
                .Include(p => p.Korisnik)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    (p.Naslov != null && p.Naslov.Contains(searchQuery)) ||
                    (p.Sadrzaj != null && p.Sadrzaj.Contains(searchQuery)) ||
                    (p.Korisnik != null && p.Korisnik.Email.Contains(searchQuery))
                );
            }

            query = query.OrderByDescending(p => p.DatumUpita);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var upiti = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

            return Json(new
            {
                upiti = jsonUpiti,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePodrskaStatus(int id, string status)
        {
            var upit = await _context.PodrskaUpiti.FindAsync(id);
            if (upit == null)
            {
                return NotFound();
            }

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

        [HttpGet]
        public async Task<JsonResult> GetRecenzijeJson(string? searchQuery = null, string? korisnikFilter = null, string? voziloFilter = null, int page = 1)
        {
            int pageSize = int.MaxValue;

            var query = _context.Recenzije
                .Include(r => r.Korisnik)
                .Include(r => r.Vozilo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(korisnikFilter))
            {
                query = query.Where(r =>
                    (r.Korisnik.UserName != null && r.Korisnik.UserName.Contains(korisnikFilter)) ||
                    (r.Korisnik.Email != null && r.Korisnik.Email.Contains(korisnikFilter)) ||
                    (r.Korisnik.Ime != null && r.Korisnik.Ime.Contains(korisnikFilter)) ||
                    (r.Korisnik.Prezime != null && r.Korisnik.Prezime.Contains(korisnikFilter))
                );
            }

            if (!string.IsNullOrEmpty(voziloFilter))
            {
                query = query.Where(r =>
                    (r.Vozilo.Marka != null && r.Vozilo.Marka.Contains(voziloFilter)) ||
                    (r.Vozilo.Model != null && r.Vozilo.Model.Contains(voziloFilter))
                );
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(r => r.Komentar != null && r.Komentar.Contains(searchQuery));
            }

            query = query.OrderByDescending(r => r.DatumRecenzije);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var recenzije = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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

            return Json(new
            {
                recenzije = jsonRecenzije,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }

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

        [HttpGet]
        public async Task<JsonResult> GetVozilaJson(string? searchQuery = null, int page = 1, string? sortOrder = null)
        {
            int pageSize = int.MaxValue;

            var query = _context.Vozila.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(v => (v.Marka != null && v.Marka.Contains(searchQuery)) || (v.Model != null && v.Model.Contains(searchQuery)));
            }

            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(v => v.Cijena);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(v => v.Cijena);
                    break;
                case "year_asc":
                    query = query.OrderBy(v => v.Godiste);
                    break;
                case "year_desc":
                    query = query.OrderByDescending(v => v.Godiste);
                    break;

                default:
                    query = query.OrderByDescending(v => v.VoziloID);
                    break;
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var vozila = await query
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync();

            var jsonVozila = vozila.Select(v => new
            {
                voziloID = v.VoziloID,
                naziv = $"{v.Marka} {v.Model}".Trim(),
                godiste = v.Godiste,
                gorivo = v.Gorivo.ToString(),
                kilometraza = v.Kilometraza,
                cijena = v.Cijena,
                boja = v.Boja,
                kubikaza = v.Kubikaza,
            }).ToList();

            return Json(new
            {
                vozila = jsonVozila,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize
            });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<JsonResult> GetProfiliJson(string? searchQuery = null, int page = 1)
        {
            int pageSize = int.MaxValue;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchQuery)) ||
                    (u.Email != null && u.Email.Contains(searchQuery)) ||
                    (u.Ime != null && u.Ime.Contains(searchQuery)) ||
                    (u.Prezime != null && u.Prezime.Contains(searchQuery))
                );
            }

            query = query.OrderBy(u => u.UserName);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var profili = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var jsonProfili = new List<object>();

            foreach (var user in profili)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string uloga = string.Join(", ", roles);

                jsonProfili.Add(new
                {
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    ime = user.Ime,
                    prezime = user.Prezime,
                    uloga = uloga
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

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAddProfilForm()
        {
            var viewModel = new AddProfilViewModel();

            var roles = await _roleManager.Roles.ToListAsync();
            viewModel.DostupneRole = roles;

            return PartialView("_AddProfilForm", viewModel);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetEditProfilForm(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new AddProfilViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Ime = user.Ime,
                Prezime = user.Prezime
            };

            var roles = await _roleManager.Roles.ToListAsync();
            viewModel.DostupneRole = roles;
            viewModel.OdabraneRole = (await _userManager.GetRolesAsync(user)).ToList();

            return PartialView("_AddProfilForm", viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfil(AddProfilViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.UserId))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

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
            else
            {
                user = await _userManager.FindByIdAsync(viewModel.UserId);
                if (user == null)
                {
                    return NotFound();
                }

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

            if (viewModel.OdabraneRole != null && viewModel.OdabraneRole.Any())
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, viewModel.OdabraneRole);
            }
            else
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Any())
                {
                    await _userManager.AddToRoleAsync(user, "Kupac");
                }
            }

            return Ok(new { userId = user.Id, successMessage = "Korisnik uspješno sačuvan!" });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfil(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var userOrders = await _context.Narudzbe
                    .Where(n => n.KorisnikId == id)
                    .ToListAsync();

                foreach (var order in userOrders)
                {
                    var payment = await _context.Placanja
                        .FirstOrDefaultAsync(p => p.NarudzbaID == order.NarudzbaID);

                    if (payment != null)
                    {
                        _context.Placanja.Remove(payment);
                    }

                    var orderItems = await _context.StavkeKorpe
                        .Where(s => s.NarudzbaID == order.NarudzbaID)
                        .ToListAsync();

                    if (orderItems.Any())
                    {
                        _context.StavkeKorpe.RemoveRange(orderItems);
                    }
                }

                await _context.SaveChangesAsync();

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
