using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Validation;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.FirstNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.FirstNameLettersOnly")]
        [Display(Name = "FirstName")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.LastNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.LastNameLettersOnly")]
        [Display(Name = "LastName")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [RegularExpression(UserInputPatterns.Email, ErrorMessage = "Validation.EmailValid")]
        [Display(Name = "EmailAddress")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation.UsernameRequired")]
        [StringLength(100, ErrorMessage = "Validation.UsernameMaxLength")]
        [RegularExpression(UserInputPatterns.Username, ErrorMessage = "Validation.UsernameAlphanumeric")]
        [Display(Name = "Username")]
        public string UserName { get; set; }
    }
}
