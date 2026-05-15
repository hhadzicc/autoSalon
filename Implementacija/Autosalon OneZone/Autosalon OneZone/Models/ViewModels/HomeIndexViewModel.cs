using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.ViewModels
{
    public class HomeIndexViewModel
    {
        public Vozilo? FeaturedVehicle { get; set; }

        public IReadOnlyList<Vozilo> FeaturedVehicles { get; set; } = Array.Empty<Vozilo>();
    }
}
