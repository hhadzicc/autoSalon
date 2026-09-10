using Microsoft.EntityFrameworkCore;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Data;
namespace Autosalon_OneZone.Services
{
    public interface IVoziloService
    {
        Task<IEnumerable<Vozilo>> GetAllVozilaAsync();
        Task<Vozilo> GetVoziloByIdAsync(int id);
        Task<Vozilo> AddVoziloAsync(Vozilo vozilo);
        Task<Vozilo> UpdateVoziloAsync(Vozilo vozilo);
        Task<bool> DeleteVoziloAsync(int id);
        Task<IEnumerable<Vozilo>> FilterVozilaAsync(string marka, string model, int? godisteOd, int? godisteDo, TipGoriva? gorivo, decimal? cijenaOd, decimal? cijenaDo);
        Task<IEnumerable<Vozilo>> SearchVozilaAsync(string searchTerm);
        Task<Vozilo?> GetVehicleDetailsAsync(int id);
        Task<IReadOnlyList<Vozilo>> GetVehiclesAsync(VehicleSearchCriteria criteria);
    }

    public class VoziloService : IVoziloService
    {
        private readonly ApplicationDbContext _context;

        public VoziloService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vozilo>> GetAllVozilaAsync()
        {
            return await _context.Vozila.ToListAsync();
        }

        public async Task<Vozilo> GetVoziloByIdAsync(int id)
        {
            return await _context.Vozila.FindAsync(id);
        }

        public async Task<Vozilo> AddVoziloAsync(Vozilo vozilo)
        {
            _context.Vozila.Add(vozilo);
            await _context.SaveChangesAsync();
            return vozilo;
        }

        public async Task<Vozilo> UpdateVoziloAsync(Vozilo vozilo)
        {
            var existingVozilo = await _context.Vozila.FindAsync(vozilo.VoziloID);

            if (existingVozilo == null)
            {
                return null;
            }

            _context.Entry(existingVozilo).CurrentValues.SetValues(vozilo);

            await _context.SaveChangesAsync();
            return existingVozilo;
        }

        public async Task<bool> DeleteVoziloAsync(int id)
        {
            var voziloToDelete = await _context.Vozila.FindAsync(id);

            if (voziloToDelete == null)
            {
                return false;
            }

            _context.Vozila.Remove(voziloToDelete);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Vozilo>> FilterVozilaAsync(string marka, string model, int? godisteOd, int? godisteDo, TipGoriva? gorivo, decimal? cijenaOd, decimal? cijenaDo)
        {
            var query = _context.Vozila.AsQueryable();

            if (!string.IsNullOrEmpty(marka))
            {
                query = query.Where(v => v.Marka.Contains(marka));
            }

            if (!string.IsNullOrEmpty(model))
            {
                query = query.Where(v => v.Model.Contains(model));
            }

            if (godisteOd.HasValue)
            {
                query = query.Where(v => v.Godiste >= godisteOd.Value);
            }

            if (godisteDo.HasValue)
            {
                query = query.Where(v => v.Godiste <= godisteDo.Value);
            }

            if (gorivo.HasValue)
            {
                query = query.Where(v => v.Gorivo == gorivo.Value);
            }

            if (cijenaOd.HasValue)
            {
                query = query.Where(v => v.Cijena >= cijenaOd.Value);
            }

            if (cijenaDo.HasValue)
            {
                query = query.Where(v => v.Cijena <= cijenaDo.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Vozilo>> SearchVozilaAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<Vozilo>();
            }

            var term = searchTerm.ToLower();

            return await _context.Vozila
                .Where(v => v.Marka.ToLower().Contains(term) ||
                            v.Model.ToLower().Contains(term))
                .ToListAsync();
        }

        public async Task<Vozilo?> GetVehicleDetailsAsync(int id)
        {
            var vehicle = await _context.Vozila
                .AsNoTracking()
                .Include(item => item.Recenzije)
                .ThenInclude(review => review.Korisnik)
                .FirstOrDefaultAsync(item => item.VoziloID == id);

            if (vehicle?.Recenzije != null)
            {
                vehicle.Recenzije = vehicle.Recenzije
                    .OrderByDescending(review => review.DatumRecenzije)
                    .ToList();
            }

            return vehicle;
        }

        public async Task<IReadOnlyList<Vozilo>> GetVehiclesAsync(VehicleSearchCriteria criteria)
        {
            IEnumerable<Vozilo> vehicles = await _context.Vozila.AsNoTracking().ToListAsync();

            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                var words = criteria.SearchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                vehicles = vehicles.Where(vehicle => words.Any(word =>
                    (vehicle.Marka?.ToLower().Contains(word) ?? false) ||
                    (vehicle.Model?.ToLower().Contains(word) ?? false)));
            }

            if (criteria.YearFrom.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Godiste >= criteria.YearFrom.Value);
            if (criteria.YearTo.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Godiste <= criteria.YearTo.Value);
            if (!string.IsNullOrEmpty(criteria.Fuel))
                vehicles = vehicles.Where(vehicle => vehicle.Gorivo.ToString() == criteria.Fuel);
            if (!string.IsNullOrEmpty(criteria.Color))
                vehicles = vehicles.Where(vehicle =>
                    vehicle.Boja != null && vehicle.Boja.Contains(criteria.Color, StringComparison.OrdinalIgnoreCase));
            if (criteria.EngineDisplacementFrom.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Kubikaza >= criteria.EngineDisplacementFrom.Value);
            if (criteria.EngineDisplacementTo.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Kubikaza <= criteria.EngineDisplacementTo.Value);
            if (criteria.MileageFrom.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Kilometraza >= criteria.MileageFrom.Value);
            if (criteria.MileageTo.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Kilometraza <= criteria.MileageTo.Value);
            if (criteria.PriceFrom.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Cijena >= criteria.PriceFrom.Value);
            if (criteria.PriceTo.HasValue)
                vehicles = vehicles.Where(vehicle => vehicle.Cijena <= criteria.PriceTo.Value);

            vehicles = criteria.SortOrder switch
            {
                "name_desc" => vehicles.OrderByDescending(vehicle => vehicle.Marka)
                    .ThenByDescending(vehicle => vehicle.Model),
                "price" => vehicles.OrderBy(vehicle => vehicle.Cijena),
                "price_desc" => vehicles.OrderByDescending(vehicle => vehicle.Cijena),
                "year" => vehicles.OrderBy(vehicle => vehicle.Godiste),
                "year_desc" => vehicles.OrderByDescending(vehicle => vehicle.Godiste),
                _ => vehicles.OrderBy(vehicle => vehicle.Marka).ThenBy(vehicle => vehicle.Model)
            };

            return vehicles.ToList();
        }
    }

    public sealed record VehicleSearchCriteria(
        string? SearchTerm,
        string? SortOrder,
        int? YearFrom,
        int? YearTo,
        string? Fuel,
        string? Color,
        decimal? EngineDisplacementFrom,
        decimal? EngineDisplacementTo,
        double? MileageFrom,
        double? MileageTo,
        decimal? PriceFrom,
        decimal? PriceTo);
}
