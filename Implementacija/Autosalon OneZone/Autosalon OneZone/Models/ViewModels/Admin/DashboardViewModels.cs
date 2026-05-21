using System;
using System.Collections.Generic;
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int BrojVozila { get; set; }
        public int BrojKorisnika { get; set; }
        public int BrojNarudzbi { get; set; }
        public int BrojAktivnihUpita { get; set; }
        public decimal UkupanPromet { get; set; }
        public List<DashboardKupovinaViewModel> ZadnjeKupovine { get; set; } = new();
        public List<DashboardUpitViewModel> ZadnjiUpiti { get; set; } = new();
        public List<DashboardRecenzijaViewModel> ZadnjeRecenzije { get; set; } = new();
    }

    public class DashboardKupovinaViewModel
    {
        public int NarudzbaID { get; set; }
        public DateTime DatumNarudzbe { get; set; }
        public string Korisnik { get; set; } = string.Empty;
        public decimal UkupanIznos { get; set; }
        public StatusNarudzbe Status { get; set; }
        public List<string> Vozila { get; set; } = new();
    }

    public class DashboardUpitViewModel
    {
        public int UpitID { get; set; }
        public DateTime DatumUpita { get; set; }
        public string Naslov { get; set; } = string.Empty;
        public string KorisnikEmail { get; set; } = string.Empty;
        public StatusUpita Status { get; set; }
    }

    public class DashboardRecenzijaViewModel
    {
        public int RecenzijaID { get; set; }
        public DateTime DatumRecenzije { get; set; }
        public string Korisnik { get; set; } = string.Empty;
        public string Vozilo { get; set; } = string.Empty;
        public int Ocjena { get; set; }
    }
}
