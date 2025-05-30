// Fajl: Models/ViewModels/CartItemViewModel.cs

namespace Autosalon_OneZone.Models.ViewModels
{
    // ViewModel za prikaz pojedinačne stavke vozila u korpi
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public string SlikaUrl { get; set; }
        public string Naziv { get; set; }
        public int Godiste { get; set; }
        public string Gorivo { get; set; }
        public decimal Cijena { get; set; }
        public int StavkaId { get; set; } // Added to fix CS0117  
        public int Kolicina { get; set; } // Added to fix CS0117  
    }
}