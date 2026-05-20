using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Trenutna lozinka je obavezna.")]
        [DataType(DataType.Password)]
        [Display(Name = "Trenutna lozinka")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Nova lozinka je obavezna.")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova lozinka")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Potvrda nove lozinke je obavezna.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potvrdi novu lozinku")]
        [Compare("NewPassword", ErrorMessage = "Nova lozinka i potvrda se ne podudaraju.")]
        public string ConfirmPassword { get; set; }
    }
}
