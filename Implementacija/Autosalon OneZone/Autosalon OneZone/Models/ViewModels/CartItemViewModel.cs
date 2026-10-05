namespace Autosalon_OneZone.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public string SlikaUrl { get; set; }
        public string Naziv { get; set; }
        public int Godiste { get; set; }
        public TipGoriva Gorivo { get; set; }
        public int Kilometraza { get; set; }
        public decimal Cijena { get; set; }
        public int StavkaId { get; set; }
        public int Kolicina { get; set; }
    }
}
