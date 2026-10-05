using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Validation;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Validation.UsernameRequired")]
        [Display(Name = "Username")]
        [MaxLength(100, ErrorMessage = "Validation.UsernameMaxLength")]
        [RegularExpression(UserInputPatterns.Username, ErrorMessage = "Validation.UsernameAlphanumeric")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [RegularExpression(UserInputPatterns.Email, ErrorMessage = "Validation.EmailValid")]
        [Display(Name = "EmailAddress")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [StringLength(100, ErrorMessage = PasswordPolicy.ErrorMessage, MinimumLength = PasswordPolicy.RequiredLength)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
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
        [MaxLength(100, ErrorMessage = "Validation.FirstNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.FirstNameLettersOnly")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [Display(Name = "LastName")]
        [MaxLength(100, ErrorMessage = "Validation.LastNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.LastNameLettersOnly")]
        public string Prezime { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Validation.LoginIdentifierRequired")]
        [MaxLength(256, ErrorMessage = "Validation.LoginIdentifierMaxLength")]
        [Display(Name = "AuthLoginIdentifier")]
        public string LoginIdentifier { get; set; }

        [Required(ErrorMessage = "Validation.PasswordRequired")]
        [DataType(DataType.Password)]
        [Display(Name = "AuthPassword")]
        public string Password { get; set; }

        [Display(Name = "AuthRememberMe")]
        public bool RememberMe { get; set; }

        public IReadOnlyList<DemoLoginAccountViewModel> DemoAccounts { get; set; } = [];
    }

    public sealed record DemoLoginAccountViewModel(
        string RoleResourceKey,
        string IconClass,
        string LoginIdentifier,
        string Password);

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [RegularExpression(UserInputPatterns.Email, ErrorMessage = "Validation.EmailValid")]
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
        [StringLength(100, ErrorMessage = PasswordPolicy.ErrorMessage, MinimumLength = PasswordPolicy.RequiredLength)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
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
