// Autosalon OneZone/Models/ViewModels/ChangePasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Trenutna lozinka je obavezna")]
        [DataType(DataType.Password)]
        [Display(Name = "Trenutna lozinka")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Nova lozinka je obavezna")]
        [StringLength(100, ErrorMessage = "{0} mora biti najmanje {2} karaktera dugačka.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova lozinka")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Potvrda nove lozinke je obavezna")]
        [DataType(DataType.Password)]
        [Display(Name = "Potvrdite novu lozinku")]
        [Compare("NewPassword", ErrorMessage = "Nova lozinka i potvrda se ne podudaraju.")]
        public string ConfirmPassword { get; set; }
    }
}
