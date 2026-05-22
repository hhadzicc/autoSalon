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

    public class AddVoziloViewModel : IValidatableObject
    {
        public int VoziloID { get; set; }

        [Required(ErrorMessage = "Validation.VehicleMakeRequired")]
        [MaxLength(100, ErrorMessage = "Validation.VehicleMakeMaxLength")]
        [Display(Name = "VehicleMake")]
        public string Marka { get; set; } = "";

        [Required(ErrorMessage = "Validation.VehicleModelRequired")]
        [MaxLength(100, ErrorMessage = "Validation.VehicleModelMaxLength")]
        [Display(Name = "VehicleModel")]
        public string Model { get; set; } = "";

        [Required(ErrorMessage = "Validation.VehicleYearRequired")]
        [Range(1900, 2026, ErrorMessage = "Validation.VehicleYearRange")]
        [Display(Name = "VehicleYear")]
        public int? Godiste { get; set; }

        [Required(ErrorMessage = "Validation.VehicleFuelRequired")]
        [Display(Name = "VehicleFuel")]
        public string Gorivo { get; set; } = "";

        [Display(Name = "VehicleDisplacement")]
        [Range(0, double.MaxValue, ErrorMessage = "Validation.VehicleDisplacementPositive")]
        public decimal? Kubikaza { get; set; }

        [Required(ErrorMessage = "Validation.VehicleColorRequired")]
        [MaxLength(50, ErrorMessage = "Validation.VehicleColorMaxLength")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s-]+$", ErrorMessage = "Validation.VehicleColorLettersOnly")]
        [Display(Name = "VehicleColor")]
        public string? Boja { get; set; }

        [Required(ErrorMessage = "Validation.VehicleMileageRequired")]
        [Range(0, double.MaxValue, ErrorMessage = "Validation.VehicleMileagePositive")]
        [Display(Name = "VehicleMileage")]
        public double? Kilometraza { get; set; }

        [Required(ErrorMessage = "Validation.VehiclePriceRequired")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Validation.VehiclePricePositive")]
        [Display(Name = "VehiclePrice")]
        public decimal? Cijena { get; set; }

        [Display(Name = "VehicleImage")]
        public IFormFile? Slika { get; set; }

        [Required(ErrorMessage = "Validation.VehicleDescriptionRequired")]
        [MaxLength(2000, ErrorMessage = "Validation.VehicleDescriptionMaxLength")]
        [Display(Name = "VehicleDescription")]
        public string? Opis { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(Gorivo, "Elektro", System.StringComparison.OrdinalIgnoreCase) &&
                !Kubikaza.HasValue)
            {
                yield return new ValidationResult(
                    "Validation.VehicleDisplacementRequired",
                    new[] { nameof(Kubikaza) });
            }

            if (Kubikaza.HasValue && Kubikaza.Value <= 0 &&
                !string.Equals(Gorivo, "Elektro", System.StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "Validation.VehicleDisplacementPositive",
                    new[] { nameof(Kubikaza) });
            }
        }
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
