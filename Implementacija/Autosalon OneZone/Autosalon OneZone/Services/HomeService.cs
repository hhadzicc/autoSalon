using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IHomeService
{
    Task<HomeIndexViewModel> GetHomePageAsync();
}

public sealed class HomeService : IHomeService
{
    private readonly ApplicationDbContext _context;

    public HomeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HomeIndexViewModel> GetHomePageAsync()
    {
        var availableVehicles = await _context.Vozila
            .AsNoTracking()
            .AvailableForPurchase()
            .ToListAsync();

        var heroOrder = new[]
        {
            "Porsche|Panamera 4 E-Hybrid",
            "BMW|X5",
            "Mercedes-Benz|C 220",
            "Audi|A4"
        };

        var featuredOrder = new[]
        {
            "Audi|e-tron GT quattro",
            "BMW|M4 Competition",
            "Mercedes-Benz|GLC 300"
        };

        var reservedFeaturedIds = availableVehicles
            .Where(vehicle => featuredOrder.Contains(VehicleKey(vehicle)))
            .Select(vehicle => vehicle.VoziloID)
            .ToHashSet();

        var heroVehicles = SelectInOrder(availableVehicles, heroOrder);
        var heroIds = heroVehicles.Select(vehicle => vehicle.VoziloID).ToHashSet();

        if (heroVehicles.Count < 4)
        {
            heroVehicles.AddRange(availableVehicles
                .Where(vehicle => !heroIds.Contains(vehicle.VoziloID) &&
                                  !reservedFeaturedIds.Contains(vehicle.VoziloID))
                .OrderByDescending(vehicle => vehicle.Cijena ?? 0)
                .ThenByDescending(vehicle => vehicle.Godiste ?? 0)
                .Take(4 - heroVehicles.Count));
        }

        heroIds = heroVehicles.Select(vehicle => vehicle.VoziloID).ToHashSet();
        var featuredVehicles = SelectInOrder(
            availableVehicles.Where(vehicle => !heroIds.Contains(vehicle.VoziloID)),
            featuredOrder);

        if (featuredVehicles.Count < 3)
        {
            var featuredIds = featuredVehicles.Select(vehicle => vehicle.VoziloID).ToHashSet();
            featuredVehicles.AddRange(availableVehicles
                .Where(vehicle => !heroIds.Contains(vehicle.VoziloID) &&
                                  !featuredIds.Contains(vehicle.VoziloID))
                .OrderByDescending(vehicle => vehicle.Cijena ?? 0)
                .ThenByDescending(vehicle => vehicle.Godiste ?? 0)
                .Take(3 - featuredVehicles.Count));
        }

        return new HomeIndexViewModel
        {
            HeroVehicles = heroVehicles,
            FeaturedVehicles = featuredVehicles
        };
    }

    private static List<Vozilo> SelectInOrder(IEnumerable<Vozilo> vehicles, IEnumerable<string> orderedKeys)
    {
        var vehiclesByKey = vehicles
            .GroupBy(VehicleKey)
            .ToDictionary(group => group.Key, group => group.First());

        return orderedKeys
            .Where(vehiclesByKey.ContainsKey)
            .Select(key => vehiclesByKey[key])
            .ToList();
    }

    private static string VehicleKey(Vozilo vehicle) => $"{vehicle.Marka}|{vehicle.Model}";

}
