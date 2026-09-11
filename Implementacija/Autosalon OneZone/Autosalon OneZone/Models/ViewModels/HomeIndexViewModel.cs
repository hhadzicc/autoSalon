using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.ViewModels
{
    public class HomeIndexViewModel
    {
        public IReadOnlyList<Vozilo> HeroVehicles { get; set; } = Array.Empty<Vozilo>();

        public IReadOnlyList<Vozilo> FeaturedVehicles { get; set; } = Array.Empty<Vozilo>();
    }
}
