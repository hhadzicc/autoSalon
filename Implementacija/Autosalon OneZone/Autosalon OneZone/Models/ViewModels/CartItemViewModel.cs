// Fajl: Models/ViewModels/CartItemViewModel.cs

namespace Autosalon_OneZone.Models.ViewModels
{
    // ViewModel za prikaz pojedinačne stavke vozila u korpi
    public class CartItemViewModel
    {
        public int Id { get; set; } // ID vozila (potrebno za uklanjanje)
        public string SlikaUrl { get; set; } // URL slike vozila
        public string Naziv { get; set; } // Naziv vozila
        public int Godiste { get; set; } // Godište vozila
        public string Gorivo { get; set; } // Tip goriva
        public decimal Cijena { get; set; } // Cena vozila
        // Dodaj ovde sva druga svojstva vozila koja želiš da prikažeš u korpi
    }
}