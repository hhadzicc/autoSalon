using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Polje Ime je obavezno.")]
        [StringLength(100, ErrorMessage = "Ime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Ime")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Polje Prezime je obavezno.")]
        [StringLength(100, ErrorMessage = "Prezime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Prezime")]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Polje Email je obavezno.")]
        [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
        [Display(Name = "Email adresa")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Polje Korisničko ime je obavezno.")]
        [StringLength(100, ErrorMessage = "Korisničko ime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Korisničko ime")]
        public string UserName { get; set; }
    }
}
