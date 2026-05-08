using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Korisničko ime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Korisničko ime može sadržavati samo slova i brojeve.")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Potvrdi lozinku")]
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne podudaraju.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Ime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Ime može sadržavati samo slova.")]
        public string Ime { get; set; }

        [Required]
        [Display(Name = "Prezime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Prezime može sadržavati samo slova.")]
        public string Prezime { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [Display(Name = "Zapamti me?")]
        public bool RememberMe { get; set; }
    }
}
