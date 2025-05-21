// Put this file in your Services folder, e.g., Autosalon_OneZone/Services/VoziloService.cs

using Microsoft.EntityFrameworkCore; // Potrebno za korišćenje DbContexta i metoda kao ToListAsync, FindAsync, etc.
using Autosalon_OneZone.Models; // Namespace gde se nalaze vaši entiteti (Vozilo, itd.)
using Autosalon_OneZone.Data;
namespace Autosalon_OneZone.Services
{
    // Interfejs za servis (dobra praksa za lakše testiranje i Dependency Injection)
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

    // Implementacija servisa
    public class VoziloService : IVoziloService
    {
        private readonly ApplicationDbContext _context; // Referenca na vaš DbContext

        // Konstruktor - DbContext se injektuje (Dependency Injection)
        public VoziloService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dohvatanje svih vozila
        public async Task<IEnumerable<Vozilo>> GetAllVozilaAsync()
        {
            return await _context.Vozila.ToListAsync();
        }

        // Dohvatanje vozila po ID-u
        public async Task<Vozilo> GetVoziloByIdAsync(int id)
        {
            // FindAsync pronalazi entitet po primarnom ključu, prvo u memoriji, zatim u bazi
            return await _context.Vozila.FindAsync(id);
        }

        // Dodavanje novog vozila
        public async Task<Vozilo> AddVoziloAsync(Vozilo vozilo)
        {
            _context.Vozila.Add(vozilo); // Dodaj entitet u DbContext praćenje promena
            await _context.SaveChangesAsync(); // Sačuvaj promene u bazi (INSERT)
            return vozilo; // Vrati dodato vozilo sa popunjenim ID-om
        }

        // Ažuriranje postojećeg vozila
        public async Task<Vozilo> UpdateVoziloAsync(Vozilo vozilo)
        {
            // Attach entitet ako nije već praćen ili uzmi entitet iz baze
            var existingVozilo = await _context.Vozila.FindAsync(vozilo.VoziloID);

            if (existingVozilo == null)
            {
                return null; // Vozilo sa datim ID-om ne postoji
            }

            // Ažuriraj svojstva postojećeg entiteta sa novim vrednostima
            _context.Entry(existingVozilo).CurrentValues.SetValues(vozilo);
            // Ili ručno prekopirajte svojstva:
            // existingVozilo.Marka = vozilo.Marka;
            // existingVozilo.Model = vozilo.Model;
            // ... ostala svojstva

            await _context.SaveChangesAsync(); // Sačuvaj promene u bazi (UPDATE)
            return existingVozilo; // Vrati ažurirano vozilo
        }

        // Brisanje vozila
        public async Task<bool> DeleteVoziloAsync(int id)
        {
            var voziloToDelete = await _context.Vozila.FindAsync(id);

            if (voziloToDelete == null)
            {
                return false; // Vozilo nije pronađeno
            }

            _context.Vozila.Remove(voziloToDelete); // Označi entitet za brisanje
            await _context.SaveChangesAsync(); // Sačuvaj promene u bazi (DELETE)

            return true; // Brisanje uspešno
        }

        // Implementacija Filtriranja vozila
        public async Task<IEnumerable<Vozilo>> FilterVozilaAsync(string marka, string model, int? godisteOd, int? godisteDo, TipGoriva? gorivo, decimal? cijenaOd, decimal? cijenaDo)
        {
            var query = _context.Vozila.AsQueryable(); // Počni sa IQueryable

            if (!string.IsNullOrEmpty(marka))
            {
                query = query.Where(v => v.Marka.Contains(marka)); // Filtriraj po marki
            }

            if (!string.IsNullOrEmpty(model))
            {
                query = query.Where(v => v.Model.Contains(model)); // Filtriraj po modelu
            }

            if (godisteOd.HasValue)
            {
                query = query.Where(v => v.Godiste >= godisteOd.Value); // Filtriraj po godištu (od)
            }

            if (godisteDo.HasValue)
            {
                query = query.Where(v => v.Godiste <= godisteDo.Value); // Filtriraj po godištu (do)
            }

            if (gorivo.HasValue)
            {
                query = query.Where(v => v.Gorivo == gorivo.Value); // Filtriraj po tipu goriva
            }

            if (cijenaOd.HasValue)
            {
                query = query.Where(v => v.Cijena >= cijenaOd.Value); // Filtriraj po ceni (od)
            }

            if (cijenaDo.HasValue)
            {
                query = query.Where(v => v.Cijena <= cijenaDo.Value); // Filtriraj po ceni (do)
            }

            return await query.ToListAsync(); // Izvrši upit i vrati rezultate
        }

        // Implementacija Pretrage vozila (po imenu ili modelu)
        public async Task<IEnumerable<Vozilo>> SearchVozilaAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<Vozilo>(); // Vrati praznu listu ako nema pojma za pretragu
            }

            var term = searchTerm.ToLower(); // Pretraga bez obzira na velika/mala slova

            return await _context.Vozila
                .Where(v => v.Marka.ToLower().Contains(term) ||
                            v.Model.ToLower().Contains(term))
                .ToListAsync(); // Izvrši pretragu
        }

    }
}