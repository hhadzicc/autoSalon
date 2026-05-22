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

        [Required(ErrorMessage = "Validation.UsernameRequired")]
        [StringLength(100, ErrorMessage = "Validation.UsernameMaxLength")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Validation.UsernameAlphanumeric")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Validation.PasswordsMatch")]
        public string? ConfirmPassword { get; set; }

        public string? Ime { get; set; }
        public string? Prezime { get; set; }

        public List<IdentityRole>? DostupneRole { get; set; }
        public List<string>? OdabraneRole { get; set; }
    }
}
