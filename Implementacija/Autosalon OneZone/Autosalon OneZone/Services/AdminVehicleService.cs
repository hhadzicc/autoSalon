using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IAdminVehicleService
{
    Task<EditVoziloViewModel?> GetForEditAsync(int id);
    Task<VehicleSaveResult> SaveAsync(AddVoziloViewModel model, TipGoriva fuel);
    Task<bool> DeleteAsync(int id);
}

public sealed class AdminVehicleService : IAdminVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AdminVehicleService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<EditVoziloViewModel?> GetForEditAsync(int id)
    {
        var vehicle = await _context.Vozila.AsNoTracking().FirstOrDefaultAsync(item => item.VoziloID == id);
        if (vehicle == null)
        {
            return null;
        }

        return new EditVoziloViewModel
        {
            VoziloID = vehicle.VoziloID,
            Marka = vehicle.Marka,
            Model = vehicle.Model,
            Godiste = vehicle.Godiste,
            Gorivo = vehicle.Gorivo.ToString(),
            Kubikaza = vehicle.Kubikaza,
            Boja = vehicle.Boja,
            Kilometraza = vehicle.Kilometraza,
            Cijena = vehicle.Cijena,
            Opis = vehicle.Opis,
            PostojecaSlikaPath = vehicle.Slika,
            ZadrzatiPostojecuSliku = true
        };
    }

    public async Task<VehicleSaveResult> SaveAsync(AddVoziloViewModel model, TipGoriva fuel)
    {
        var isNew = model.VoziloID == 0;
        var vehicle = isNew
            ? new Vozilo()
            : await _context.Vozila.FindAsync(model.VoziloID);

        if (vehicle == null)
        {
            return VehicleSaveResult.NotFound();
        }

        string? newImageName = null;
        string? newImagePath = null;
        var oldImageName = vehicle.Slika;

        try
        {
            if (model.Slika != null)
            {
                var uploadsFolder = VehicleImagesFolder();
                Directory.CreateDirectory(uploadsFolder);
                newImageName = $"{Guid.NewGuid():N}_{Path.GetFileName(model.Slika.FileName)}";
                newImagePath = Path.Combine(uploadsFolder, newImageName);

                await using var stream = new FileStream(newImagePath, FileMode.CreateNew);
                await model.Slika.CopyToAsync(stream);
            }

            vehicle.Marka = model.Marka;
            vehicle.Model = model.Model;
            vehicle.Godiste = model.Godiste;
            vehicle.Gorivo = fuel;
            vehicle.Kubikaza = model.Kubikaza;
            vehicle.Boja = model.Boja;
            vehicle.Kilometraza = model.Kilometraza;
            vehicle.Cijena = model.Cijena;
            vehicle.Opis = model.Opis;

            if (newImageName != null)
            {
                vehicle.Slika = newImageName;
            }

            if (isNew)
            {
                _context.Vozila.Add(vehicle);
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            DeleteImage(newImagePath);
            throw;
        }

        if (newImageName != null && !string.IsNullOrEmpty(oldImageName))
        {
            DeleteImage(Path.Combine(VehicleImagesFolder(), oldImageName));
        }

        return VehicleSaveResult.Saved(vehicle.VoziloID, isNew);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vehicle = await _context.Vozila.FindAsync(id);
        if (vehicle == null)
        {
            return false;
        }

        var cartItems = await _context.StavkeKorpe
            .Where(item => item.VoziloID == id && item.KorpaID != null)
            .ToListAsync();

        if (cartItems.Count > 0)
        {
            var cartIds = cartItems
                .Where(item => item.KorpaID.HasValue)
                .Select(item => item.KorpaID!.Value)
                .Distinct()
                .ToList();
            var carts = await _context.Korpe.Where(cart => cartIds.Contains(cart.KorpaID)).ToListAsync();

            foreach (var cart in carts)
            {
                var removedValue = cartItems
                    .Where(item => item.KorpaID == cart.KorpaID)
                    .Sum(item => item.CijenaStavke * item.Kolicina);
                cart.UkupnaCijena = Math.Max(0, cart.UkupnaCijena - removedValue);
            }
        }

        var imagePath = string.IsNullOrEmpty(vehicle.Slika)
            ? null
            : Path.Combine(VehicleImagesFolder(), vehicle.Slika);

        _context.Vozila.Remove(vehicle);
        await _context.SaveChangesAsync();
        DeleteImage(imagePath);
        return true;
    }

    private string VehicleImagesFolder() =>
        Path.Combine(_environment.WebRootPath, "images", "vozila");

    private static void DeleteImage(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

public sealed record VehicleSaveResult(bool Found, int VehicleId, bool IsNew)
{
    public static VehicleSaveResult Saved(int vehicleId, bool isNew) => new(true, vehicleId, isNew);
    public static VehicleSaveResult NotFound() => new(false, 0, false);
}
