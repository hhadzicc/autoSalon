using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.FirstNameMaxLength")]
        [Display(Name = "FirstName")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.LastNameMaxLength")]
        [Display(Name = "LastName")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [Display(Name = "EmailAddress")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Validation.UsernameRequired")]
        [StringLength(100, ErrorMessage = "Validation.UsernameMaxLength")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Validation.UsernameAlphanumeric")]
        [Display(Name = "Username")]
        public string UserName { get; set; }
    }
}
