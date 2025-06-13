// Put this file in your Models/ViewModels folder, e.g., Autosalon_OneZone/Models/ViewModels/AccountViewModels.cs

using System.ComponentModel.DataAnnotations; // Potrebno za Data Annotations

namespace Autosalon_OneZone.Models.ViewModels // Prilagodite Namespace
{
    // ViewModel za formu za registraciju
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Korisničko ime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Korisničko ime može sadržavati samo slova i brojeve.")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress] // Validacija formata emaila
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 8)]
        [DataType(DataType.Password)] // Ovo koristi browser da prikaže polje kao lozinku
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Potvrdi lozinku")]
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne podudaraju.")] // Proverava da li se poklapa sa poljem "Password"
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

    // ViewModel za formu za login
    public class LoginViewModel
    {
        [Required]
        [EmailAddress] // Ili samo Display(Name = "Korisničko ime") ako koristite UserName
        [Display(Name = "Email")] // Ili "Korisničko ime"
        public string Email { get; set; } // Ili Username

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [Display(Name = "Zapamti me?")]
        public bool RememberMe { get; set; }
    }
}