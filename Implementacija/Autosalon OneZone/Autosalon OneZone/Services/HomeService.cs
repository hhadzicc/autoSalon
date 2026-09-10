using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IHomeService
{
    Task<HomeIndexViewModel> GetHomePageAsync();
    Task AddSupportRequestAsync(string userId, string title, string content);
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
        var featuredVehicle = await _context.Vozila
            .AsNoTracking()
            .FirstOrDefaultAsync(vehicle =>
                vehicle.Marka == "Porsche" &&
                vehicle.Model == "Panamera 4 E-Hybrid");

        var curatedVehicles = await _context.Vozila
            .AsNoTracking()
            .Where(vehicle =>
                (vehicle.Marka == "Audi" && vehicle.Model == "e-tron GT quattro") ||
                (vehicle.Marka == "BMW" && vehicle.Model == "M4 Competition") ||
                (vehicle.Marka == "Mercedes-Benz" && vehicle.Model == "GLC 300"))
            .ToListAsync();

        var curatedOrder = new Dictionary<string, int>
        {
            ["Audi|e-tron GT quattro"] = 0,
            ["BMW|M4 Competition"] = 1,
            ["Mercedes-Benz|GLC 300"] = 2
        };
        var featuredVehicles = curatedVehicles
            .OrderBy(vehicle => curatedOrder.GetValueOrDefault(
                $"{vehicle.Marka}|{vehicle.Model}",
                int.MaxValue))
            .Take(3)
            .ToList();

        if (featuredVehicles.Count < 3)
        {
            var excludedIds = featuredVehicles.Select(vehicle => vehicle.VoziloID).ToList();
            if (featuredVehicle != null)
            {
                excludedIds.Add(featuredVehicle.VoziloID);
            }

            var fallbackVehicles = await _context.Vozila
                .AsNoTracking()
                .Where(vehicle => !excludedIds.Contains(vehicle.VoziloID))
                .OrderByDescending(vehicle => vehicle.Godiste ?? 0)
                .ThenByDescending(vehicle => vehicle.Cijena ?? 0)
                .Take(3 - featuredVehicles.Count)
                .ToListAsync();
            featuredVehicles.AddRange(fallbackVehicles);
        }

        return new HomeIndexViewModel
        {
            FeaturedVehicle = featuredVehicle,
            FeaturedVehicles = featuredVehicles
                .OrderByDescending(vehicle => vehicle.Godiste ?? 0)
                .ThenByDescending(vehicle => vehicle.Cijena ?? 0)
                .Take(3)
                .ToList()
        };
    }

    public async Task AddSupportRequestAsync(string userId, string title, string content)
    {
        _context.PodrskaUpiti.Add(new Podrska
        {
            KorisnikId = userId,
            Naslov = title,
            Sadrzaj = content,
            DatumUpita = DateTime.Now,
            Status = StatusUpita.Poslat
        });
        await _context.SaveChangesAsync();
    }
}
