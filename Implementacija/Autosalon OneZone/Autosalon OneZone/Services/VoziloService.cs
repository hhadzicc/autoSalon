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
    }
}
