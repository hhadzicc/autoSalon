// Fajl: Models/ViewModels/ProfileViewModel.cs

using System.Collections.Generic;
using Autosalon_OneZone.Models; // Potrebno ako ReviewViewModel koristi neki model iz ovog namespace-a

namespace Autosalon_OneZone.Models.ViewModels
{
    // ViewModel za prikaz korisnickog profila
    public class ProfileViewModel
    {
        public string ImePrezime { get; set; } // Ime i Prezime korisnika
        public string Email { get; set; } // Email adresa
        public string UserName { get; set; } // Korisnicko ime
        public string Role { get; set; } // Rola korisnika (mozda samo jedna glavna za prikaz)

        // Lista recenzija korisnika
        // Koristimo novu (ugnjezdenu ili zasebnu) ViewModel klasu za prikaz podataka o recenziji
        public List<ReviewViewModel> Recenzije { get; set; }


        // ViewModel za prikaz pojedinacne recenzije unutar profila
        // Mozete ovu klasu definisati ovde (ugnjezdena) ili u zasebnom fajlu ReviewViewModel.cs
        public class ReviewViewModel
        {
            public string VoziloNaziv { get; set; } // Naziv vozila na koje se recenzija odnosi
            public int Ocena { get; set; } // Ocena (npr. 1 do 5)
            public string Tekst { get; set; } // Tekst recenzije
            // Dodajte druga svojstva recenzije ako su potrebna za prikaz (npr. Datum)
        }
    }
}