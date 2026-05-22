using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Validation.UsernameRequired")]
        [Display(Name = "Username")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Validation.UsernameAlphanumeric")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [Display(Name = "EmailAddress")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        [DataType(DataType.Password)]
        [Display(Name = "AuthPassword")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Validation.ConfirmPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmPassword")]
        [Compare("Password", ErrorMessage = "Validation.PasswordsMatch")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [Display(Name = "FirstName")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Validation.FirstNameLettersOnly")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [Display(Name = "LastName")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Validation.LastNameLettersOnly")]
        public string Prezime { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Validation.LoginIdentifierRequired")]
        [MaxLength(256)]
        [Display(Name = "AuthLoginIdentifier")]
        public string LoginIdentifier { get; set; }

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "AuthPassword")]
        public string Password { get; set; }

        [Display(Name = "AuthRememberMe")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [Display(Name = "EmailAddress")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }

        [Required(ErrorMessage = "Validation.NewPasswordRequired")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Validation.ConfirmNewPasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "ConfirmNewPassword")]
        [Compare("Password", ErrorMessage = "Validation.NewPasswordsMatch")]
        public string ConfirmPassword { get; set; }
    }
}
