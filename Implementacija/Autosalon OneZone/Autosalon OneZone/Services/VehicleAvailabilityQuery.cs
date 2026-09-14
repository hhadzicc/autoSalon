using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Services;

public static class VehicleAvailabilityQuery
{
    public static IQueryable<Vozilo> AvailableForPurchase(this IQueryable<Vozilo> vehicles) =>
        vehicles.Where(vehicle =>
            !vehicle.StavkeKorpe.Any(item => item.NarudzbaID != null));

    public static IQueryable<Vozilo> UnavailableForPurchase(this IQueryable<Vozilo> vehicles) =>
        vehicles.Where(vehicle =>
            vehicle.StavkeKorpe.Any(item => item.NarudzbaID != null));
}
