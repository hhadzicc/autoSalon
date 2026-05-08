using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Autosalon_OneZone.ViewModels.Admin
{
    public class VoziloListViewModel
    {
        public List<Autosalon_OneZone.Models.Vozilo>? Vozila { get; set; }
        public string? SearchQuery { get; set; }
        public string? SortOrder { get; set; }
        public int? CurrentPage { get; set; }

        public int TotalPages { get; set; } = 0;
    }

    public class AddVoziloViewModel
    {
        public int VoziloID { get; set; }

        [Required(ErrorMessage = "Marka vozila ne smije biti prazna.")]
        [MaxLength(100, ErrorMessage = "Marka ne može biti duža od 100 karaktera.")]
        [Display(Name = "Marka")]
        public string Marka { get; set; } = "";

        [Required(ErrorMessage = "Model vozila ne smije biti prazan.")]
        [MaxLength(100, ErrorMessage = "Model ne može biti duži od 100 karaktera.")]
        [Display(Name = "Model")]
        public string Model { get; set; } = "";

        [Required(ErrorMessage = "Godište je obavezno.")]
        [Range(1900, 2025, ErrorMessage = "Godište mora biti između 1900 i 2025.")]
        [Display(Name = "Godište")]
        public int? Godiste { get; set; }

        [Required(ErrorMessage = "Gorivo je obavezno.")]
        [Display(Name = "Gorivo")]
        public string Gorivo { get; set; } = "";

        [Required(ErrorMessage = "Kubikaža ne smije biti prazna.")]
        [Range(1, double.MaxValue, ErrorMessage = "Kubikaža mora biti pozitivan broj.")]
        [Display(Name = "Kubikaža")]
        public decimal? Kubikaza { get; set; }

        [Required(ErrorMessage = "Boja ne smije biti prazna.")]
        [MaxLength(50, ErrorMessage = "Boja ne može biti duža od 50 karaktera.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s-]+$", ErrorMessage = "Boja može sadržavati samo slova.")]
        [Display(Name = "Boja")]
        public string? Boja { get; set; }

        [Required(ErrorMessage = "Kilometraža je obavezna.")]
        [Range(0, double.MaxValue, ErrorMessage = "Kilometraža ne može biti negativna.")]
        [Display(Name = "Kilometraža")]
        public double? Kilometraza { get; set; }

        [Required(ErrorMessage = "Cijena je obavezna.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cijena mora biti veća od nule.")]
        [Display(Name = "Cijena")]
        public decimal? Cijena { get; set; }

        [Display(Name = "Slika")]
        public IFormFile? Slika { get; set; }

        [Required(ErrorMessage = "Opis ne smije biti prazan.")]
        [MaxLength(2000, ErrorMessage = "Opis ne može biti duži od 2000 karaktera.")]
        [Display(Name = "Opis")]
        public string? Opis { get; set; }
    }

    public class EditVoziloViewModel : AddVoziloViewModel
    {
        public string? PostojecaSlikaPath { get; set; }
        public bool ZadrzatiPostojecuSliku { get; set; } = true;
    }

    public class VoziloDetailsViewModel
    {
        public Autosalon_OneZone.Models.Vozilo? Vozilo { get; set; }
        public List<Autosalon_OneZone.Models.Recenzija>? Recenzije { get; set; }

        public double ProsjecnaOcjena { get; set; } = 0;
    }
}
