using System.Collections.Generic;
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string ImePrezime { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }

        public List<ReviewViewModel> Recenzije { get; set; }

        public class ReviewViewModel
        {
            public string VoziloNaziv { get; set; }
            public int Ocena { get; set; }
            public string Tekst { get; set; }
        }
    }
}
