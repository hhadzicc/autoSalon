using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Models;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.ViewModels.Admin
{
    public class ProfilListViewModel
    {
        public List<ApplicationUser>? Profili { get; set; }
        public string? SearchQuery { get; set; }
    }

    public class AddProfilViewModel
    {
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Korisničko ime je obavezno.")]
        [StringLength(100, ErrorMessage = "Korisničko ime ne može biti duže od 100 karaktera.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Korisničko ime može sadržavati samo slova i brojeve.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Lozinka i potvrda lozinke se ne podudaraju.")]
        public string? ConfirmPassword { get; set; }

        public string? Ime { get; set; }
        public string? Prezime { get; set; }

        public List<IdentityRole>? DostupneRole { get; set; }
        public List<string>? OdabraneRole { get; set; }
    }
}
