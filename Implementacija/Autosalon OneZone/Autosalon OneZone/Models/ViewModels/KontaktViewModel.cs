using System.ComponentModel.DataAnnotations;

namespace Autosalon_OneZone.ViewModels
{
    public class KontaktViewModel
    {
        [Required(ErrorMessage = "Validation.SubjectRequired")]
        [StringLength(200, ErrorMessage = "Validation.SubjectMaxLength")]
        [Display(Name = "ContactSubject")]
        public string Naslov { get; set; }

        [Required(ErrorMessage = "Validation.MessageRequired")]
        [StringLength(5000, ErrorMessage = "Validation.MessageMaxLength")]
        [Display(Name = "ContactMessage")]
        public string Sadrzaj { get; set; }
    }
}
