// Fajl: Models/ViewModels/CartViewModel.cs

using System.Collections.Generic;
// Dodaj using ako CartItemViewModel.cs nije u istom namespace-u ili folderu
// using Autosalon_OneZone.Models.ViewModels;

namespace Autosalon_OneZone.Models.ViewModels
{
    // ViewModel za prikaz cele stranice korpe
    public class CartViewModel
    {
        // Lista vozila koja se nalaze u korpi
        public List<CartItemViewModel> VozilaUKorpi { get; set; }

        // Ukupna cena svih vozila u korpi
        public decimal UkupnaCijena { get; set; }

        // Opciono: Možeš definisati CartItemViewModel kao Ugnježdenu (Nested) klasu ovde
        /*
        public class CartItemViewModel
        {
            public int Id { get; set; }
            public string SlikaUrl { get; set; }
            public string Naziv { get; set; }
            public int Godiste { get; set; }
            public string Gorivo { get; set; }
            public decimal Cijena { get; set; }
        }
        */
    }
}