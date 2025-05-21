using System.Collections.Generic;
using Autosalon_OneZone.Models; // Uključite vaš model Podrska
using System;

namespace Autosalon_OneZone.ViewModels.Admin
{
    // ViewModel za prikaz liste upita za podršku
    public class PodrskaListViewModel
    {
        public List<Podrska> Upiti { get; set; }
        public string SearchQuery { get; set; } // Pretraga po tekstu upita, emailu, itd.
    }
}