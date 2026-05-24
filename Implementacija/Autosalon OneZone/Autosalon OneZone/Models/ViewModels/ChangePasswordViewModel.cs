using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Validation;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Validation.CurrentPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Validation.NewPasswordRequired")]
        [StringLength(100, ErrorMessage = PasswordPolicy.ErrorMessage, MinimumLength = PasswordPolicy.RequiredLength)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
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
