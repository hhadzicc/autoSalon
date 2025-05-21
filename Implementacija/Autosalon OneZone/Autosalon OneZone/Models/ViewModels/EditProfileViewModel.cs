// Fajl: Models/ViewModels/EditProfileViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.Models.ViewModels
{
    // ViewModel za formu za izmenu korisničkog profila (BEZ polja za promenu lozinke)
    public class EditProfileViewModel
    {
        // --- Osnovne informacije o profilu ---
        // Ovi podaci se popunjavaju iz baze (ApplicationUser) i prikazuju na formi za izmenu
        // [Required] i StringLength se koriste za serversku validaciju podataka koje korisnik unese
        [Required(ErrorMessage = "Polje Ime je obavezno.")]
        [StringLength(100, ErrorMessage = "Ime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Ime")] // Tekst koji se prikazuje uz polje (ako koristite asp-label)
        public string Ime { get; set; }

        [Required(ErrorMessage = "Polje Prezime je obavezno.")]
        [StringLength(100, ErrorMessage = "Prezime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Prezime")]
        public string Prezime { get; set; }

        // Email adresa - takođe obavezna i mora biti validan format
        [Required(ErrorMessage = "Polje Email je obavezno.")]
        [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
        [Display(Name = "Email adresa")]
        public string Email { get; set; }

        // Korisničko ime - obavezno
        // Napomena: Ako Vam je UserName isto sto i Email (kao u default Identity), ovo polje mozda ne treba menjati ovde
        [Required(ErrorMessage = "Polje Korisničko ime je obavezno.")]
        [StringLength(100, ErrorMessage = "Korisničko ime ne može biti duže od 100 karaktera.")]
        [Display(Name = "Korisničko ime")]
        public string UserName { get; set; }

    }
}