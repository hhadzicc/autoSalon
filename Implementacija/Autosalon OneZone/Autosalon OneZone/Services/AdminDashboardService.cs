using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(string currentUserId, bool isAdministrator);
}

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(string currentUserId, bool isAdministrator)
    {
        var recentPurchases = await _context.Narudzbe
            .AsNoTracking()
            .Include(order => order.Korisnik)
            .Include(order => order.StavkeKorpe)
            .ThenInclude(item => item.Vozilo)
            .OrderByDescending(order => order.DatumNarudzbe)
            .Take(5)
            .ToListAsync();

        var visibleSupportRequests = _context.PodrskaUpiti
            .AsNoTracking()
            .Where(request => request.Status != StatusUpita.Zatvoren);

        if (!isAdministrator)
        {
            visibleSupportRequests = visibleSupportRequests.Where(request =>
                request.DodijeljenKorisnikId == null ||
                request.DodijeljenKorisnikId == currentUserId);
        }

        var recentSupportRequests = await visibleSupportRequests
            .Include(request => request.Korisnik)
            .OrderBy(request => request.Status == StatusUpita.CekaPodrsku
                ? 0
                : request.Status == StatusUpita.UObradi ? 1 : 2)
            .ThenByDescending(request => request.DatumZadnjeAktivnosti)
            .Take(5)
            .ToListAsync();

        var recentReviews = await _context.Recenzije
            .AsNoTracking()
            .Include(review => review.Korisnik)
            .Include(review => review.Vozilo)
            .OrderByDescending(review => review.DatumRecenzije)
            .Take(5)
            .ToListAsync();

        var vehicleCount = await _context.Vozila.CountAsync();
        var availableVehicleCount = await _context.Vozila.AvailableForPurchase().CountAsync();

        return new AdminDashboardViewModel
        {
            IsAdministrator = isAdministrator,
            BrojVozila = vehicleCount,
            BrojDostupnihVozila = availableVehicleCount,
            BrojProdatihVozila = vehicleCount - availableVehicleCount,
            BrojKorisnika = isAdministrator ? await _context.Users.CountAsync() : 0,
            BrojNarudzbi = isAdministrator ? await _context.Narudzbe.CountAsync() : 0,
            BrojAktivnihUpita = await visibleSupportRequests.CountAsync(),
            BrojMojihAktivnihUpita = isAdministrator
                ? 0
                : await _context.PodrskaUpiti.CountAsync(request =>
                    request.Status != StatusUpita.Zatvoren &&
                    request.DodijeljenKorisnikId == currentUserId),
            UkupanPromet = isAdministrator
                ? await _context.Narudzbe
                    .Where(order => order.Status != StatusNarudzbe.Otkazana)
                    .SumAsync(order => (decimal?)order.UkupnaCijena) ?? 0
                : 0,
            ZadnjeKupovine = recentPurchases.Select(order => new DashboardKupovinaViewModel
            {
                NarudzbaID = order.NarudzbaID,
                DatumNarudzbe = order.DatumNarudzbe,
                Korisnik = UserDisplayName(order.Korisnik),
                Status = order.Status,
                UkupanIznos = order.UkupnaCijena,
                Vozila = order.StavkeKorpe
                    .Select(item => VehicleDisplayName(item.Vozilo))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList()
            }).ToList(),
            ZadnjiUpiti = recentSupportRequests.Select(request => new DashboardUpitViewModel
            {
                UpitID = request.UpitID,
                DatumUpita = request.DatumZadnjeAktivnosti,
                Naslov = request.Naslov,
                KorisnikEmail = request.Korisnik?.Email ?? "N/A",
                Status = request.Status
            }).ToList(),
            ZadnjeRecenzije = recentReviews.Select(review => new DashboardRecenzijaViewModel
            {
                RecenzijaID = review.RecenzijaID,
                DatumRecenzije = review.DatumRecenzije,
                Korisnik = UserDisplayName(review.Korisnik),
                Vozilo = VehicleDisplayName(review.Vozilo),
                Ocjena = review.Ocjena
            }).ToList()
        };
    }

    private static string UserDisplayName(ApplicationUser? user)
    {
        if (user == null)
        {
            return "N/A";
        }

        var fullName = $"{user.Ime} {user.Prezime}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.Email ?? user.UserName ?? "N/A";
    }

    private static string VehicleDisplayName(Vozilo? vehicle) =>
        vehicle == null ? string.Empty : $"{vehicle.Marka} {vehicle.Model}".Trim();
}
