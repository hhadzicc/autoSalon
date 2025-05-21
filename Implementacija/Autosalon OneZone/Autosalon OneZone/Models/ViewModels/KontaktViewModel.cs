// Autosalon OneZone/ViewModels/KontaktViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.ViewModels
{
    public class KontaktViewModel
    {
        [Required(ErrorMessage = "Naslov je obavezan")]
        [StringLength(200, ErrorMessage = "Naslov ne može biti duži od 200 karaktera")]
        [Display(Name = "Naslov")]
        public string Naslov { get; set; }

        [Required(ErrorMessage = "Poruka je obavezna")]
        [Display(Name = "Poruka")]
        public string Sadrzaj { get; set; }
    }
}
