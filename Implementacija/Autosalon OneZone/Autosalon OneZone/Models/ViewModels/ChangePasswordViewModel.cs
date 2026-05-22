using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Validation.CurrentPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Validation.NewPasswordRequired")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Validation.ConfirmNewPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmNewPassword")]
        [Compare("NewPassword", ErrorMessage = "Validation.NewPasswordsMatch")]
        public string ConfirmPassword { get; set; }
    }
}
