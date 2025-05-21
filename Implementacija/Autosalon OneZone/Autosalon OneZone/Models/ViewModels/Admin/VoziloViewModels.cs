using System;
using System.Collections.Generic; // Mozda ne treba, ali ne skodi
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Za IFormFile

// Ovisno gdje se nalaze TipGoriva i Vozilo entitet:
// using Autosalon_OneZone.Models; // Ako su u Models folderu i ovom namespace-u
// Ili specifičnije: using Autosalon_OneZone.Models.DomainEntities; // Ako su u fajlu DomainEntities.cs sa tim namespace-om

namespace Autosalon_OneZone.ViewModels.Admin // Provjerite da li je ovo ispravan namespace za ViewModele
{
    public class VoziloListViewModel
    {
        public List<Autosalon_OneZone.Models.Vozilo>? Vozila { get; set; } // Uklonjen warning CS8618: dodan '?'
        public string? SearchQuery { get; set; } // Uklonjen warning CS8618: dodan '?'
        public string? SortOrder { get; set; } // Uklonjen warning CS8618: dodan '?'
        public int? CurrentPage { get; set; }
        // public int TotalPages { get; set; } // Uklonjen warning CS8618: ako nije inicijalizovan, dodati '= 0;' ili ?
        public int TotalPages { get; set; } = 0; // Primjer inicijalizacije za int

    }

    public class AddVoziloViewModel
    {
        public int VoziloID { get; set; } // 0 za Add, > 0 za Edit

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


    // Keep EditVoziloViewModel inheriting if it only adds image handling
    public class EditVoziloViewModel : AddVoziloViewModel
    {
        public string? PostojecaSlikaPath { get; set; } // Path do postojeće slike za Edit
        public bool ZadrzatiPostojecuSliku { get; set; } = true; // Flag za edit
    }

    // Ostali ViewModeli kao prije
    public class VoziloDetailsViewModel
    {
        public Autosalon_OneZone.Models.Vozilo? Vozilo { get; set; } // Uklonjen warning CS8618: dodan '?'
        public List<Autosalon_OneZone.Models.Recenzija>? Recenzije { get; set; } // Uklonjen warning CS8618: dodan '?'
                                                                                 // public double ProsjecnaOcjena { get; set; } // Uklonjen warning CS8618: ako nije inicijalizovan, dodati '= 0;' ili ?
        public double ProsjecnaOcjena { get; set; } = 0; // Primjer inicijalizacije za double
    }
}