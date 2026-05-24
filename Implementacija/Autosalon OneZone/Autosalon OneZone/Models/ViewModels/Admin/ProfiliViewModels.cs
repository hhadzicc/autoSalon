using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Validation;
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
        [RegularExpression(UserInputPatterns.Username, ErrorMessage = "Validation.UsernameAlphanumeric")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.EmailRequired")]
        [EmailAddress(ErrorMessage = "Validation.EmailValid")]
        [RegularExpression(UserInputPatterns.Email, ErrorMessage = "Validation.EmailValid")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = PasswordPolicy.ErrorMessage, MinimumLength = PasswordPolicy.RequiredLength)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Validation.PasswordsMatch")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Validation.FirstNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.FirstNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.FirstNameLettersOnly")]
        public string? Ime { get; set; }

        [Required(ErrorMessage = "Validation.LastNameRequired")]
        [StringLength(100, ErrorMessage = "Validation.LastNameMaxLength")]
        [RegularExpression(UserInputPatterns.PersonName, ErrorMessage = "Validation.LastNameLettersOnly")]
        public string? Prezime { get; set; }

        public List<IdentityRole>? DostupneRole { get; set; }
        public List<string>? OdabraneRole { get; set; }
    }
}
