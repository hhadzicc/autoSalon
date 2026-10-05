using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Autosalon_OneZone.Validation;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.Models
{
    public enum TipGoriva
    {
        Benzin,
        Dizel,
        Plin,
        Elektro,
        Hibrid
    }

    public enum TipBoje
    {
        Bijela,
        Crna,
        Siva,
        Srebrna,
        Zlatna,
        Bez,
        Smedja,
        Crvena,
        Plava,
        Zelena,
        Zuta,
        Narandzasta,
        Ljubicasta,
        Roza,
        Visebojna,
        Ostalo
    }

    public enum StatusNarudzbe
    {
        Kreirana,
        Placena,
        UObradi,
        Isporucena,
        Otkazana
    }

    public enum StatusUpita
    {
        CekaPodrsku,
        UObradi,
        CekaKorisnika,
        Rijesen,
        Zatvoren
    }

    public enum TipAutoraPorukePodrske
    {
        Korisnik,
        Osoblje,
        Sistem
    }

    public enum StatusPlacanja
    {
        Obrada,
        Uspjesno,
        Odbijeno,
        Otkazano,
        Neuspjesno
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(UserInputPatterns.PersonName)]
        public string Ime { get; set; }

        [Required]
        [MaxLength(100)]
        [RegularExpression(UserInputPatterns.PersonName)]
        public string Prezime { get; set; }

        public Korpa Korpa { get; set; }

        public ICollection<Narudzba> Narudzbe { get; set; }

        public ICollection<Recenzija> Recenzije { get; set; }

        public ICollection<Podrska> PodrskaUpiti { get; set; }

        public ICollection<Podrska> DodijeljeniUpiti { get; set; }

        public ICollection<PorukaPodrske> PorukePodrske { get; set; }

        public ApplicationUser()
        {
            Narudzbe = new HashSet<Narudzba>();
            Recenzije = new HashSet<Recenzija>();
            PodrskaUpiti = new HashSet<Podrska>();
            DodijeljeniUpiti = new HashSet<Podrska>();
            PorukePodrske = new HashSet<PorukaPodrske>();
        }
    }

    public class Vozilo
    {
        [Key]
        public int VoziloID { get; set; }

        public string Marka { get; set; }

        public string Model { get; set; }

        public int? Godiste { get; set; }

        public TipGoriva Gorivo { get; set; }

        public decimal? Kubikaza { get; set; }

        public TipBoje Boja { get; set; }

        public int? Kilometraza { get; set; }
        public int? Cijena { get; set; }
        public string? Slika { get; set; }
        public string? Opis { get; set; }

        public ICollection<StavkaKorpe> StavkeKorpe { get; set; }

        public ICollection<Recenzija> Recenzije { get; set; }

        public Vozilo()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>();
            Recenzije = new HashSet<Recenzija>();
        }
    }

    public class Korpa
    {
        [Key]
        public int KorpaID { get; set; }

        [Required]
        public string KorisnikId { get; set; }

        public ApplicationUser Korisnik { get; set; }

        public decimal UkupnaCijena { get; set; }

        public ICollection<StavkaKorpe> StavkeKorpe { get; set; }

        public Korpa()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>();
        }
    }

    public class StavkaKorpe
    {
        [Key]
        public int StavkaID { get; set; }

        [Required]
        public int Kolicina { get; set; }

        public decimal CijenaStavke { get; set; }

        [Required]
        public int VoziloID { get; set; }

        public int? KorpaID { get; set; }

        public int? NarudzbaID { get; set; }

        public Vozilo Vozilo { get; set; }

        public Korpa Korpa { get; set; }

        public Narudzba Narudzba { get; set; }
    }

    public class Narudzba
    {
        [Key]
        public int NarudzbaID { get; set; }

        [Required]
        public DateTime DatumNarudzbe { get; set; }

        [Required]
        public StatusNarudzbe Status { get; set; }

        [Required]
        public decimal UkupnaCijena { get; set; }

        [Required]
        public string KorisnikId { get; set; }

        public ApplicationUser Korisnik { get; set; }

        public ICollection<StavkaKorpe> StavkeKorpe { get; set; }

        public Placanje Placanje { get; set; }

        public Narudzba()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>();
        }
    }

    public class Placanje
    {
        [Key]
        public int NarudzbaID { get; set; }

        public Narudzba Narudzba { get; set; }

        [Required]
        public DateTime DatumPlacanja { get; set; }

        [Required]
        public decimal Iznos { get; set; }

        [Required]
        public StatusPlacanja Status { get; set; }

        public int? KarticaID { get; set; }

        public int? KreditID { get; set; }

        public Kartica Kartica { get; set; }

        public Kredit Kredit { get; set; }
    }

    public class Kartica
    {
        [Key]
        public int KarticaID { get; set; }

        [Required]
        [CreditCard]
        [MaxLength(20)]
        public string BrojKartice { get; set; }

        [Required]
        [MaxLength(5)]
        public string DatumIsteka { get; set; }

        [Required]
        [MaxLength(4)]
        public string Cvv { get; set; }

        [Required]
        [MaxLength(200)]
        public string ImeVlasnika { get; set; }
    }

    public class Kredit
    {
        [Key]
        public int KreditID { get; set; }

        [Required]
        public decimal Iznos { get; set; }

        [Required]
        public int BrojRata { get; set; }

        [Required]
        public decimal KamatnaStopa { get; set; }

        public decimal MjesecnaRata { get; set; }
    }

    public class Recenzija
    {
        [Key]
        public int RecenzijaID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Ocjena { get; set; }

        [MaxLength(1000)]
        public string Komentar { get; set; }

        [Required]
        public DateTime DatumRecenzije { get; set; }

        [Required]
        public string KorisnikId { get; set; }

        [Required]
        public int VoziloID { get; set; }

        public ApplicationUser Korisnik { get; set; }

        public Vozilo Vozilo { get; set; }
    }

    public class Podrska
    {
        [Key]
        public int UpitID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Naslov { get; set; }

        [Required]
        public DateTime DatumUpita { get; set; }

        [Required]
        public DateTime DatumZadnjeAktivnosti { get; set; }

        [Required]
        public StatusUpita Status { get; set; }

        [Required]
        public string KorisnikId { get; set; }

        public ApplicationUser Korisnik { get; set; }

        public string? DodijeljenKorisnikId { get; set; }

        public ApplicationUser? DodijeljenKorisnik { get; set; }

        public DateTime? DatumDodjele { get; set; }

        public DateTime? DatumRjesavanja { get; set; }

        public DateTime? DatumZatvaranja { get; set; }

        public DateTime? DatumPodsjetnika { get; set; }

        [Required]
        [MaxLength(10)]
        public string Jezik { get; set; } = "en-US";

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<PorukaPodrske> Poruke { get; set; } = new HashSet<PorukaPodrske>();
    }

    public class PorukaPodrske
    {
        [Key]
        public int PorukaPodrskeID { get; set; }

        [Required]
        public int UpitID { get; set; }

        public Podrska Upit { get; set; }

        public string? PosiljalacId { get; set; }

        public ApplicationUser? Posiljalac { get; set; }

        [Required]
        public TipAutoraPorukePodrske TipAutora { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Sadrzaj { get; set; }

        [Required]
        public DateTime DatumSlanja { get; set; }

        public DateTime? ProcitanaUtc { get; set; }

        public EmailPodrskeOutbox? EmailOutbox { get; set; }
    }

    public class EmailPodrskeOutbox
    {
        [Key]
        public int EmailPodrskeOutboxID { get; set; }

        [Required]
        public int PorukaPodrskeID { get; set; }

        public PorukaPodrske Poruka { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Primalac { get; set; }

        [Required]
        [MaxLength(10)]
        public string Jezik { get; set; } = "en-US";

        [Required]
        public DateTime KreiranoUtc { get; set; }

        [Required]
        public DateTime SljedeciPokusajUtc { get; set; }

        public int BrojPokusaja { get; set; }

        public DateTime? PoslanoUtc { get; set; }

        [MaxLength(2000)]
        public string? ZadnjaGreska { get; set; }

        [Required]
        [MaxLength(2048)]
        public string DetaljiUrl { get; set; }
    }
}
