using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface ICartService
{
    Task<CartAddResult> AddVehicleAsync(string userId, int vehicleId);
    Task<int> GetItemCountAsync(string userId);
    Task<HashSet<int>> GetVehicleIdsAsync(string userId, IEnumerable<int> vehicleIds);
    Task<CartViewModel> GetCartAsync(string userId);
    Task<CartRemoveResult> RemoveVehicleAsync(string userId, int vehicleId);
    Task ClearAsync(string userId);
}

public sealed class CartService : ICartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartAddResult> AddVehicleAsync(string userId, int vehicleId)
    {
        var vehicle = await _context.Vozila
            .AvailableForPurchase()
            .FirstOrDefaultAsync(item => item.VoziloID == vehicleId);
        if (vehicle == null)
        {
            var vehicleExists = await _context.Vozila
                .AsNoTracking()
                .AnyAsync(item => item.VoziloID == vehicleId);
            return vehicleExists
                ? CartAddResult.VehicleAlreadyPurchased
                : CartAddResult.VehicleNotFound;
        }

        var cart = await _context.Korpe
            .Include(k => k.StavkeKorpe)
            .FirstOrDefaultAsync(k => k.KorisnikId == userId);

        if (cart == null)
        {
            cart = new Korpa
            {
                KorisnikId = userId,
                UkupnaCijena = 0
            };
            _context.Korpe.Add(cart);
        }

        if (cart.StavkeKorpe.Any(item => item.VoziloID == vehicleId))
        {
            return CartAddResult.AlreadyAdded;
        }

        var price = vehicle.Cijena ?? 0;
        cart.StavkeKorpe.Add(new StavkaKorpe
        {
            VoziloID = vehicleId,
            Kolicina = 1,
            CijenaStavke = price
        });
        cart.UkupnaCijena += price;

        await _context.SaveChangesAsync();
        return CartAddResult.Added;
    }

    public async Task<CartViewModel> GetCartAsync(string userId)
    {
        var cart = await _context.Korpe
            .AsNoTracking()
            .Include(k => k.StavkeKorpe)
            .ThenInclude(item => item.Vozilo)
            .FirstOrDefaultAsync(k => k.KorisnikId == userId);

        if (cart == null)
        {
            return new CartViewModel
            {
                VozilaUKorpi = new List<CartItemViewModel>(),
                UkupnaCijena = 0
            };
        }

        var purchasedVehicleIds = await _context.StavkeKorpe
            .AsNoTracking()
            .Where(item => item.NarudzbaID != null)
            .Select(item => item.VoziloID)
            .Distinct()
            .ToListAsync();
        var availableItems = cart.StavkeKorpe
            .Where(item => !purchasedVehicleIds.Contains(item.VoziloID))
            .ToList();

        return new CartViewModel
        {
            VozilaUKorpi = availableItems.Select(item => new CartItemViewModel
            {
                Id = item.VoziloID,
                StavkaId = item.StavkaID,
                Naziv = $"{item.Vozilo.Marka} {item.Vozilo.Model}",
                SlikaUrl = !string.IsNullOrEmpty(item.Vozilo.Slika)
                    ? $"/images/vozila/{item.Vozilo.Slika}"
                    : "/img/no-image.png",
                Godiste = item.Vozilo.Godiste ?? 0,
                Gorivo = item.Vozilo.Gorivo.ToString(),
                Cijena = item.CijenaStavke,
                Kolicina = item.Kolicina
            }).ToList(),
            UkupnaCijena = availableItems.Sum(item => item.CijenaStavke * item.Kolicina)
        };
    }

    public Task<int> GetItemCountAsync(string userId)
    {
        return _context.StavkeKorpe
            .AsNoTracking()
            .CountAsync(item =>
                item.Korpa != null &&
                item.Korpa.KorisnikId == userId &&
                item.NarudzbaID == null);
    }

    public async Task<HashSet<int>> GetVehicleIdsAsync(string userId, IEnumerable<int> vehicleIds)
    {
        var ids = vehicleIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new HashSet<int>();
        }

        var cartVehicleIds = await _context.StavkeKorpe
            .AsNoTracking()
            .Where(item =>
                item.Korpa != null &&
                item.Korpa.KorisnikId == userId &&
                item.NarudzbaID == null &&
                ids.Contains(item.VoziloID))
            .Select(item => item.VoziloID)
            .Distinct()
            .ToListAsync();

        return cartVehicleIds.ToHashSet();
    }

    public async Task<CartRemoveResult> RemoveVehicleAsync(string userId, int vehicleId)
    {
        var cart = await _context.Korpe
            .Include(k => k.StavkeKorpe)
            .FirstOrDefaultAsync(k => k.KorisnikId == userId);

        if (cart == null)
        {
            return CartRemoveResult.CartNotFound;
        }

        var item = cart.StavkeKorpe.FirstOrDefault(entry => entry.VoziloID == vehicleId);
        if (item == null)
        {
            return CartRemoveResult.VehicleNotFound;
        }

        cart.UkupnaCijena = Math.Max(0, cart.UkupnaCijena - item.CijenaStavke * item.Kolicina);
        _context.StavkeKorpe.Remove(item);
        await _context.SaveChangesAsync();

        return CartRemoveResult.Removed;
    }

    public async Task ClearAsync(string userId)
    {
        var cart = await _context.Korpe
            .Include(k => k.StavkeKorpe)
            .FirstOrDefaultAsync(k => k.KorisnikId == userId);

        if (cart == null)
        {
            return;
        }

        _context.StavkeKorpe.RemoveRange(cart.StavkeKorpe);
        cart.UkupnaCijena = 0;
        await _context.SaveChangesAsync();
    }
}

public enum CartAddResult
{
    Added,
    AlreadyAdded,
    VehicleAlreadyPurchased,
    VehicleNotFound
}

public enum CartRemoveResult
{
    Removed,
    VehicleNotFound,
    CartNotFound
}
