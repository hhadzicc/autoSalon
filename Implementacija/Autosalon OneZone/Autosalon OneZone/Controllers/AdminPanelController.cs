using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Validation;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Autosalon_OneZone.Authorization;
using Autosalon_OneZone.Services;

namespace Autosalon_OneZone.Controllers
{
    [Authorize(Roles = AppRoles.AdministratorOrSeller)]
    public class AdminPanelController : Controller
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IAdminDashboardService _dashboardService;
        private readonly IAdminListQueryService _listQueryService;
        private readonly IAdminModerationService _moderationService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly IAdminProfileService _profileService;


        public AdminPanelController(
            IAdminDashboardService dashboardService,
            IAdminListQueryService listQueryService,
            IAdminModerationService moderationService,
            IAdminVehicleService vehicleService,
            IAdminProfileService profileService,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
            _dashboardService = dashboardService;
            _listQueryService = listQueryService;
            _moderationService = moderationService;
            _vehicleService = vehicleService;
            _profileService = profileService;
        }

        [HttpGet]
        public IActionResult Index(int? editVehicleId = null, string? returnUrl = null)
        {
            var currentSection = HttpContext.Request.Query["section"].ToString();
            if (editVehicleId.HasValue)
            {
                currentSection = "Vozila";
            }

            if (string.IsNullOrEmpty(currentSection))
            {
                currentSection = "Dashboard";
            }

            ViewBag.CurrentSection = currentSection;
            ViewBag.EditVehicleId = editVehicleId;
            ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardSection()
        {
            return PartialView("_AdminDashboard", await _dashboardService.GetDashboardAsync());
        }


        #region Vozila sekcija

        [HttpGet]
        public async Task<IActionResult> GetVozilaSection(string searchQuery = null)
        {
            return PartialView("_AdminVozila", await _listQueryService.GetVehicleSectionAsync(searchQuery));
        }

        [HttpGet]
        public IActionResult GetAddVoziloForm()
        {
            return PartialView("_AddVoziloForm", new AddVoziloViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> GetEditVoziloForm(int id)
        {
            var viewModel = await _vehicleService.GetForEditAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return PartialView("_AddVoziloForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveVozilo(AddVoziloViewModel viewModel)
        {
            if (viewModel.Godiste.HasValue && !VehicleYearPolicy.IsValid(viewModel.Godiste.Value))
            {
                ModelState.Remove(nameof(viewModel.Godiste));
                ModelState.AddModelError(
                    nameof(viewModel.Godiste),
                    _localizer[
                        "Validation.VehicleYearRange",
                        VehicleYearPolicy.MinimumYear,
                        VehicleYearPolicy.MaximumYear]);
            }

            if (viewModel.VoziloID == 0 && viewModel.Slika == null)
            {
                ModelState.AddModelError("Slika", _localizer["VehicleImageRequiredNew"]);
            }

            if (viewModel.VoziloID > 0)
            {
                ModelState.Remove("Slika");
            }

            if (viewModel.Slika != null)
            {
                var imageValidation = VehicleImageValidator.Validate(viewModel.Slika);

                if (!imageValidation.HasAllowedExtension)
                {
                    ModelState.AddModelError("Slika", _localizer["AllowedImageExtensionsError"]);
                }

                if (!imageValidation.HasValidContent)
                {
                    ModelState.AddModelError("Slika", _localizer["InvalidImageContentError"]);
                }

                if (!imageValidation.IsWithinSizeLimit)
                {
                    ModelState.AddModelError("Slika", _localizer["ImageSizeLimitError"]);
                }
            }

            if (!Enum.TryParse<TipGoriva>(viewModel.Gorivo, ignoreCase: true, out var gorivo) ||
                !Enum.IsDefined(typeof(TipGoriva), gorivo))
            {
                ModelState.AddModelError("Gorivo", _localizer["InvalidFuelValue"]);
            }
            else if (gorivo == TipGoriva.Elektro)
            {
                ModelState.Remove(nameof(viewModel.Kubikaza));
                viewModel.Kubikaza = null;
            }

            if (!viewModel.Boja.HasValue || !Enum.IsDefined(typeof(TipBoje), viewModel.Boja.Value))
            {
                ModelState.Remove(nameof(viewModel.Boja));
                ModelState.AddModelError(nameof(viewModel.Boja), _localizer["InvalidColorValue"]);
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
            var result = await _vehicleService.SaveAsync(viewModel, gorivo);
            if (!result.Found)
            {
                return NotFound();
            }

            return Ok(new
            {
                voziloId = result.VehicleId,
                successMessage = viewModel.VoziloID > 0 ? _localizer["VehicleUpdateSuccess"].Value : _localizer["VehicleCreateSuccess"].Value
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVozilo(int id)
        {
            if (!await _vehicleService.DeleteAsync(id))
            {
                return NotFound();
            }

            return Ok(new { successMessage = _localizer["VehicleDeletedSuccess"].Value });
        }

        #endregion

        #region Profili sekcija

        [HttpGet]
        [Authorize(Roles = AppRoles.Administrator)]
        public async Task<IActionResult> GetProfiliSection(string? searchQuery = null)
        {
            return PartialView("_AdminProfili", await _listQueryService.GetProfileSectionAsync(searchQuery));
        }
        #endregion
        #region Recenzije sekcija

        [HttpGet]
        public async Task<IActionResult> GetRecenzijeSection(string? searchQuery = null)
        {
            return PartialView("_AdminRecenzije", await _listQueryService.GetReviewSectionAsync(searchQuery));
        }

        [HttpGet]
        public async Task<JsonResult> GetReviewCustomerSuggestions(string? query = null, string? selectedId = null)
        {
            return Json(new
            {
                options = await _listQueryService.GetReviewCustomerSuggestionsAsync(query, selectedId)
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetReviewVehicleSuggestions(string? query = null, int? selectedId = null)
        {
            return Json(new
            {
                options = await _listQueryService.GetReviewVehicleSuggestionsAsync(query, selectedId)
            });
        }
        #endregion
        #region Podrška sekcija

        [HttpGet]
        public IActionResult GetPodrskaSection(string? searchQuery = null)
        {
            var viewModel = new Autosalon_OneZone.ViewModels.Admin.PodrskaListViewModel
            {
                SearchQuery = searchQuery
            };

            return PartialView("_AdminPodrska", viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetPodrskaJson(string? searchQuery = null, int page = 1, int? offset = null)
        {
            var result = await _listQueryService.GetSupportAsync(searchQuery, page, offset);

            return Json(new
            {
                upiti = result.Items,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize = result.PageSize,
                offset = result.Offset,
                hasMore = result.Offset + result.Items.Count < result.TotalCount
            });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdministratorOrSeller)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePodrska(int id)
        {
            if (!await _moderationService.DeleteSupportRequestAsync(id))
            {
                return NotFound();
            }

            return Ok(new { successMessage = _localizer["SupportDeleteSuccess"].Value });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.AdministratorOrSeller)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePodrskaStatus(int id, string status)
        {
            var result = await _moderationService.UpdateSupportStatusAsync(id, status);
            if (result == SupportStatusUpdateResult.NotFound)
            {
                return NotFound();
            }

            if (result == SupportStatusUpdateResult.Updated)
            {
                return Ok(new { successMessage = _localizer["StatusChangedSuccess"].Value });
            }

            return BadRequest(_localizer["InvalidStatus"].Value);
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetRecenzijeJson(
            string? searchQuery = null,
            string? korisnikIdFilter = null,
            string? korisnikFilter = null,
            int? voziloIdFilter = null,
            string? voziloFilter = null,
            int? ocjenaFilter = null,
            string? sort = null,
            string? direction = null,
            int page = 1,
            int? offset = null)
        {
            var result = await _listQueryService.GetReviewsAsync(
                searchQuery,
                korisnikIdFilter,
                korisnikFilter,
                voziloIdFilter,
                voziloFilter,
                ocjenaFilter,
                sort,
                direction,
                page,
                offset);

            return Json(new
            {
                recenzije = result.Items,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize = result.PageSize,
                offset = result.Offset,
                hasMore = result.Offset + result.Items.Count < result.TotalCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecenzija(int id)
        {
            if (!await _moderationService.DeleteReviewAsync(id))
            {
                return NotFound();
            }

            return Ok(new { successMessage = _localizer["ReviewDeletedSuccess"].Value });
        }

        [HttpGet]
        public async Task<JsonResult> GetVozilaJson(
            string? searchQuery = null,
            int page = 1,
            string? sortOrder = null,
            string? gorivoFilter = null,
            string? bojaFilter = null,
            string? statusFilter = null,
            string? sort = null,
            string? direction = null,
            int? offset = null)
        {
            var result = await _listQueryService.GetVehiclesAsync(
                searchQuery,
                sortOrder,
                gorivoFilter,
                bojaFilter,
                statusFilter,
                sort,
                direction,
                page,
                offset);

            return Json(new
            {
                vozila = result.Items,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize = result.PageSize,
                offset = result.Offset,
                hasMore = result.Offset + result.Items.Count < result.TotalCount,
                sort = result.Sort,
                direction = result.Direction
            });
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.Administrator)]
        public async Task<JsonResult> GetProfiliJson(
            string? searchQuery = null,
            int page = 1,
            string? roleFilter = null,
            string? userIdFilter = null,
            int? offset = null)
        {
            var result = await _listQueryService.GetProfilesAsync(searchQuery, roleFilter, userIdFilter, page, offset);

            return Json(new
            {
                profili = result.Items,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                pageSize = result.PageSize,
                offset = result.Offset,
                hasMore = result.Offset + result.Items.Count < result.TotalCount
            });
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.Administrator)]
        public async Task<IActionResult> GetAddProfilForm()
        {
            return PartialView("_AddProfilForm", await _profileService.GetCreateModelAsync());
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.Administrator)]
        public async Task<IActionResult> GetEditProfilForm(string id)
        {
            var viewModel = await _profileService.GetEditModelAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return PartialView("_AddProfilForm", viewModel);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Administrator)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfil(AddProfilViewModel viewModel)
        {
            var isEdit = !string.IsNullOrEmpty(viewModel.UserId);

            if (isEdit)
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");

                if (!string.IsNullOrWhiteSpace(viewModel.Password))
                {
                    if (string.IsNullOrWhiteSpace(viewModel.ConfirmPassword))
                    {
                        ModelState.AddModelError(nameof(viewModel.ConfirmPassword), _localizer["NewPasswordConfirmationRequired"]);
                    }
                    else if (viewModel.Password != viewModel.ConfirmPassword)
                    {
                        ModelState.AddModelError(nameof(viewModel.ConfirmPassword), _localizer["NewPasswordMismatch"]);
                    }
                    else if (!PasswordPolicy.IsValid(viewModel.Password))
                    {
                        ModelState.AddModelError(nameof(viewModel.Password), _localizer["PasswordPolicyError"]);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(viewModel.ConfirmPassword))
                {
                    ModelState.AddModelError(nameof(viewModel.Password), _localizer["PasswordFieldsEitherBothOrEmpty"]);
                }
            }
            else if (string.IsNullOrWhiteSpace(viewModel.Password))
            {
                ModelState.AddModelError(nameof(viewModel.Password), _localizer["PasswordRequired"]);
            }
            else if (!PasswordPolicy.IsValid(viewModel.Password))
            {
                ModelState.AddModelError(nameof(viewModel.Password), _localizer["PasswordPolicyError"]);
            }
            else if (string.IsNullOrWhiteSpace(viewModel.ConfirmPassword))
            {
                ModelState.AddModelError(nameof(viewModel.ConfirmPassword), _localizer["ConfirmPasswordRequired"]);
            }

            if (viewModel.OdabraneRole != null && viewModel.OdabraneRole.Count == 0 && Request.Form["OdabraneRole"].Count > 0)
            {
                viewModel.OdabraneRole = new List<string> { Request.Form["OdabraneRole"].ToString() };
            }

            var roleValidation = await _profileService.ValidateRolesAsync(viewModel.OdabraneRole);
            viewModel.OdabraneRole = roleValidation.ValidRoles;

            if (roleValidation.InvalidRoles.Any())
            {
                ModelState.AddModelError(nameof(viewModel.OdabraneRole), $"Nevalidne uloge: {string.Join(", ", roleValidation.InvalidRoles)}");
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

            var result = await _profileService.SaveAsync(
                viewModel,
                User.FindFirstValue(ClaimTypes.NameIdentifier));
            return result.Status switch
            {
                ProfileSaveStatus.Saved => Ok(new
                {
                    userId = result.UserId,
                    successMessage = _localizer["UserSavedSuccess"].Value
                }),
                ProfileSaveStatus.NotFound => NotFound(),
                ProfileSaveStatus.UsernameExists => BadRequest(new
                {
                    identityErrors = new[] { _localizer["UsernameExists"].Value }
                }),
                ProfileSaveStatus.EmailExists => BadRequest(new
                {
                    identityErrors = new[] { _localizer["EmailExists"].Value }
                }),
                ProfileSaveStatus.CannotRemoveOwnAdministratorRole => BadRequest(new
                {
                    identityErrors = new[] { "Ne mozete ukloniti Administrator ulogu sa vlastitog naloga." }
                }),
                ProfileSaveStatus.CannotRemoveOnlyAdministratorRole => BadRequest(new
                {
                    identityErrors = new[] { "Nije moguce ukloniti Administrator ulogu sa jedinog administratorskog naloga." }
                }),
                _ => BadRequest(new { identityErrors = result.Errors ?? Array.Empty<string>() })
            };
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Administrator)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfil(string id)
        {
            try
            {
                var result = await _profileService.DeleteAsync(
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier));
                return result.Status switch
                {
                    ProfileDeleteStatus.Deleted => Ok(new
                    {
                        successMessage = _localizer["UserDeletedSuccess"].Value
                    }),
                    ProfileDeleteStatus.NotFound => NotFound(),
                    ProfileDeleteStatus.CannotDeleteOwnAdministratorAccount => BadRequest(new
                    {
                        errorMessage = "Ne mozete obrisati vlastiti administratorski nalog."
                    }),
                    _ => BadRequest(result.Errors ?? Array.Empty<string>())
                };
            }
            catch (Exception ex)
            {
                return BadRequest(new { errorMessage = string.Format(_localizer["UserDeleteFailure"].Value, ex.Message) });
            }
        }
    }
}
