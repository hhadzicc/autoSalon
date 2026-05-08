using System.Collections.Generic;
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.ViewModels.Admin
{
    public class RecenzijaListViewModel
    {
        public List<Recenzija> Recenzije { get; set; }
        public string SearchQuery { get; set; }
        public string KorisnikFilter { get; set; }
        public string VoziloFilter { get; set; }
    }
}
