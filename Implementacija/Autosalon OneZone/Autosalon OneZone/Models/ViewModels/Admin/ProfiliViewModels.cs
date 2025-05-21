using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.ViewModels.Admin
{
    // ViewModel za prikaz liste profila
    public class ProfilListViewModel
    {
        public List<ApplicationUser>? Profili { get; set; }
        public string? SearchQuery { get; set; }
    }

    // ViewModel za formu za dodavanje/uređivanje korisnika
    public class AddProfilViewModel
    {
        // ID korisnika (null za nove korisnike)
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Korisničko ime je obavezno.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
        public string Email { get; set; } = string.Empty;

        // Lozinka je obavezna samo za nove korisnike
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje {2} znakova.", MinimumLength = 6)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne podudaraju.")]
        public string? ConfirmPassword { get; set; }

        // Dodatna polja za korisnika
        public string? Ime { get; set; }
        public string? Prezime { get; set; }

        // Svojstva za uloge
        public List<IdentityRole>? DostupneRole { get; set; }
        public List<string>? OdabraneRole { get; set; }
    }
}
