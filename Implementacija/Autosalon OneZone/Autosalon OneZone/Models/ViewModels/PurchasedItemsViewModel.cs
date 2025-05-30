// Models/ViewModels/PurchasedItemsViewModel.cs
using System;
using System.Collections.Generic;

namespace Autosalon_OneZone.Models.ViewModels
{
    public class PurchasedItemsViewModel
    {
        public List<PurchasedItemViewModel> PurchasedItems { get; set; } = new List<PurchasedItemViewModel>();

        public class PurchasedItemViewModel
        {
            public int VoziloID { get; set; }
            public string Naziv { get; set; }
            public string Slika { get; set; }
            public decimal Cijena { get; set; }
            public DateTime DatumKupovine { get; set; }
            public int? NarudzbaID { get; set; }
            public RecenzijaViewModel Recenzija { get; set; }
            public bool HasReview => Recenzija != null;
        }

        public class RecenzijaViewModel
        {
            public int RecenzijaID { get; set; }
            public int Ocjena { get; set; }
            public string Komentar { get; set; }
            public DateTime DatumRecenzije { get; set; }
        }
    }
}
