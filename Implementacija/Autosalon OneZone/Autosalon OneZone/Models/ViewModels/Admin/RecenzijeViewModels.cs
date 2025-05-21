using System.Collections.Generic;
using Autosalon_OneZone.Models; // Uključite vaš model Recenzija

namespace Autosalon_OneZone.ViewModels.Admin
{
    // ViewModel za prikaz liste recenzija
    public class RecenzijaListViewModel
    {
        public List<Recenzija> Recenzije { get; set; }
        public string SearchQuery { get; set; } // Pretraga po tekstu recenzije
        public string KorisnikFilter { get; set; } // Filtriranje po korisniku
        public string VoziloFilter { get; set; } // Filtriranje po vozilu
    }
}