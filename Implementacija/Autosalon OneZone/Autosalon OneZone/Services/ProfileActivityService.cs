using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IProfileActivityService
{
    Task<ProfileViewModel> GetProfileAsync(ApplicationUser user, string role);
    Task<PurchasedItemsViewModel> GetPurchasedItemsAsync(string userId);
    Task<ReviewSaveResult> SaveReviewAsync(string userId, int vehicleId, int rating, string comment);
    Task<int?> GetReviewIdAsync(string userId, int vehicleId);
    Task<bool> DeleteReviewAsync(string userId, int reviewId);
}

public sealed class ProfileActivityService : IProfileActivityService
{
    private readonly ApplicationDbContext _context;

    public ProfileActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProfileViewModel> GetProfileAsync(ApplicationUser user, string role)
    {
        return Task.FromResult(new ProfileViewModel
        {
            ImePrezime = $"{user.Ime} {user.Prezime}",
            Email = user.Email,
            UserName = user.UserName,
            Role = role
        });
    }

    public async Task<PurchasedItemsViewModel> GetPurchasedItemsAsync(string userId)
    {
        var orders = await _context.Narudzbe
            .AsNoTracking()
            .Where(order => order.KorisnikId == userId)
            .Include(order => order.StavkeKorpe)
            .ThenInclude(item => item.Vozilo)
            .OrderByDescending(order => order.DatumNarudzbe)
            .ToListAsync();
        var reviews = await _context.Recenzije
            .AsNoTracking()
            .Where(review => review.KorisnikId == userId)
            .ToListAsync();
        var uniqueVehicles = new Dictionary<int, PurchasedItemsViewModel.PurchasedItemViewModel>();

        foreach (var order in orders)
        {
            foreach (var item in order.StavkeKorpe)
            {
                if (uniqueVehicles.TryGetValue(item.VoziloID, out var existingItem) &&
                    order.DatumNarudzbe <= existingItem.DatumKupovine)
                {
                    continue;
                }

                var review = reviews.FirstOrDefault(entry => entry.VoziloID == item.VoziloID);
                uniqueVehicles[item.VoziloID] = new PurchasedItemsViewModel.PurchasedItemViewModel
                {
                    VoziloID = item.VoziloID,
                    Naziv = $"{item.Vozilo.Marka} {item.Vozilo.Model}",
                    Slika = !string.IsNullOrEmpty(item.Vozilo.Slika)
                        ? $"/images/vozila/{item.Vozilo.Slika}"
                        : "/img/no-image.png",
                    Cijena = item.CijenaStavke,
                    DatumKupovine = order.DatumNarudzbe,
                    NarudzbaID = order.NarudzbaID,
                    Recenzija = review == null
                        ? null
                        : new PurchasedItemsViewModel.RecenzijaViewModel
                        {
                            RecenzijaID = review.RecenzijaID,
                            Ocjena = review.Ocjena,
                            Komentar = review.Komentar,
                            DatumRecenzije = review.DatumRecenzije
                        }
                };
            }
        }

        return new PurchasedItemsViewModel
        {
            PurchasedItems = uniqueVehicles.Values.ToList()
        };
    }

    public async Task<ReviewSaveResult> SaveReviewAsync(
        string userId,
        int vehicleId,
        int rating,
        string comment)
    {
        var hasPurchased = await _context.Narudzbe
            .Where(order =>
                order.KorisnikId == userId &&
                (order.Status == StatusNarudzbe.Placena || order.Status == StatusNarudzbe.Isporucena))
            .SelectMany(order => order.StavkeKorpe)
            .AnyAsync(item => item.VoziloID == vehicleId);
        if (!hasPurchased)
        {
            return ReviewSaveResult.NotPurchased;
        }

        var review = await _context.Recenzije
            .FirstOrDefaultAsync(entry => entry.KorisnikId == userId && entry.VoziloID == vehicleId);
        if (review == null)
        {
            _context.Recenzije.Add(new Recenzija
            {
                KorisnikId = userId,
                VoziloID = vehicleId,
                Ocjena = rating,
                Komentar = comment,
                DatumRecenzije = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return ReviewSaveResult.Added;
        }

        review.Ocjena = rating;
        review.Komentar = comment;
        review.DatumRecenzije = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ReviewSaveResult.Updated;
    }

    public async Task<int?> GetReviewIdAsync(string userId, int vehicleId)
    {
        return await _context.Recenzije
            .Where(review => review.KorisnikId == userId && review.VoziloID == vehicleId)
            .Select(review => (int?)review.RecenzijaID)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteReviewAsync(string userId, int reviewId)
    {
        var review = await _context.Recenzije
            .FirstOrDefaultAsync(entry => entry.RecenzijaID == reviewId && entry.KorisnikId == userId);
        if (review == null)
        {
            return false;
        }

        _context.Recenzije.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}

public enum ReviewSaveResult
{
    Added,
    Updated,
    NotPurchased
}
