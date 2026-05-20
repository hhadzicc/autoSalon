using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Korisnicko ime je obavezno.")]
        [Display(Name = "Korisnicko ime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Korisnicko ime moze sadrzavati samo slova i brojeve.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email adresa je obavezna.")]
        [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} karaktera.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora sadrzavati najmanje jednu cifru, jedno malo i jedno veliko slovo.")]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potvrdi lozinku")]
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne podudaraju.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Ime je obavezno.")]
        [Display(Name = "Ime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Ime moze sadrzavati samo slova.")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [Display(Name = "Prezime")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Prezime moze sadrzavati samo slova.")]
        public string Prezime { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-mail ili korisnicko ime je obavezno.")]
        [MaxLength(256)]
        [Display(Name = "E-mail ili korisnicko ime")]
        public string LoginIdentifier { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [DataType(DataType.Password)]
        [Display(Name = "Lozinka")]
        public string Password { get; set; }

        [Display(Name = "Zapamti me?")]
        public bool RememberMe { get; set; }
    }
}
