// Put this file in your Models folder, e.g., Autosalon_OneZone/Models/DomainEntities.cs

using System; // Potrebno za DateTime, Nullable types (?)
using System.Collections.Generic; // Potrebno za ICollection
using System.ComponentModel.DataAnnotations; // Potrebno za [Key], [Required], [MaxLength]
using Microsoft.AspNetCore.Identity; // Potrebno za ApplicationUser

namespace Autosalon_OneZone.Models // Vaš Namespace
{
    // --- Enumeracije (Enums) ---
    // Prethodno kreirane enumi, dodate za kompletnost u ovom fajlu

    // Enumeracija za vrstu korisničkog računa (Ako ipak zadržite ovaj koncept van Identity Rola)
    // Preporuka je koristiti ASP.NET Core Identity Role umesto ovoga za aut/auth
    public enum VrstaRacuna
    {
        Administrator,
        Prodavac,
        Kupac,
        Gost // Gosti obično nemaju "nalog" u pravom smislu, već su anonimni korisnici
    }

    // Enumeracija za tip goriva vozila
    public enum TipGoriva
    {
        Benzin,
        Dizel,
        Plin,
        Elektro,
        Hibrid
    }

    // Enumeracija za status narudžbe
    public enum StatusNarudzbe
    {
        Kreirana,
        Placena,
        UObradi,
        Isporucena,
        Otkazana
    }

    // Enumeracija za status upita podrške
    public enum StatusUpita
    {
        Poslat,
        UObradi,
        Odgovoren,
        Zatvoren
    }

    // Enumeracija za status plaćanja
    public enum StatusPlacanja
    {
        Obrada, // Plaćanje u procesu
        Uspjesno,
        Odbijeno,
        Otkazano, // Plaćanje otkazano pre obrade ili od strane korisnika
        Neuspjesno // Nešto drugo pođe po zlu tokom obrade
    }


    // --- Entitetske Klase ---

    // Zamenjuje staru Korisnik klasu, koristi ASP.NET Core Identity
    public class ApplicationUser : IdentityUser // Nasleđuje standardna Identity polja (Id, UserName, Email, itd.)
    {
        // Dodatna polja za korisnika
        [Required] // Primer: Ime je obavezno
        [MaxLength(100)] // Primer: Maksimalna dužina imena
        public string Ime { get; set; }

        [Required] // Primer: Prezime je obavezno
        [MaxLength(100)] // Primer: Maksimalna dužina prezimena
        public string Prezime { get; set; }

        // Možete dodati i druga polja ovde po potrebi
        // public DateTime DatumRegistracije { get; set; }
        // public string Adresa { get; set; }
        // itd.

        // Navigaciona svojstva koja povezuju ApplicationUser sa drugim entitetima
        // Na osnovu relacija iz vašeg class diagrama

        // Relacija 1 (ApplicationUser) -> 0..1 (Korpa)
        // Jedan ApplicationUser ima jednu Korpu (opciono, ali PK=FK konfiguracija ga čini 'zavisnim' od Korisnika)
        public Korpa Korpa { get; set; } // Jedna Korpa

        // Relacija 1 (ApplicationUser) -> 0..* (Narudzba)
        // Jedan ApplicationUser može imati mnogo Narudzbi
        public ICollection<Narudzba> Narudzbe { get; set; } // Kolekcija Narudzbi

        // Relacija 1 (ApplicationUser) -> 0..* (Recenzija)
        // Jedan ApplicationUser može napisati mnogo Recenzija
        public ICollection<Recenzija> Recenzije { get; set; } // Kolekcija Recenzija

        // Relacija 1 (ApplicationUser) -> 0..* (Podrska)
        // Jedan ApplicationUser može poslati mnogo upita za Podršku
        public ICollection<Podrska> PodrskaUpiti { get; set; } // Kolekcija Podrska upita


        // Konstruktor za inicijalizaciju kolekcija (dobra praksa)
        public ApplicationUser()
        {
            Narudzbe = new HashSet<Narudzba>();
            Recenzije = new HashSet<Recenzija>();
            PodrskaUpiti = new HashSet<Podrska>();
            // Korpa je 1-na-1 (PK=FK), ne inicijalizuje se kao kolekcija
        }
    }


    // Entitet za Vozilo (Proizvod)
    public class Vozilo
    {
        [Key]
        public int VoziloID { get; set; }

        // Dodajte 'required' modifikator ili inicijalizujte ako ne dozvoljavate null u bazi
        // [Required] // Dodajte ako marka MORA postojati
        public string Marka { get; set; } // Warning CS8618 ako nije inicijalizovan/required

        // [Required] // Dodajte ako model MORA postojati
        public string Model { get; set; } // Warning CS8618 ako nije inicijalizovan/required

        public int? Godiste { get; set; } // Nullable int je ok

        // [Required] // Dodajte ako gorivo MORA postojati
        public TipGoriva Gorivo { get; set; } // Enum je value type, non-nullable default je prva vrijednost (0)

        public decimal? Kubikaza { get; set; }

        // [Required] // Dodajte ako boja MORA postojati
        public string Boja { get; set; } // Warning CS8618 ako nije inicijalizovan/required

        public double? Kilometraza { get; set; } // Nullable double je ok
        public decimal? Cijena { get; set; } // Nullable decimal je ok
        public string? Slika { get; set; } // Nullable string je ok
        public string? Opis { get; set; } // Nullable string je ok

        // === DODAJTE OVA NAVIGACIONA SVOJSTVA ===
        // Relacija 1 (Vozilo) -> 0..* (StavkaKorpe)
        public ICollection<StavkaKorpe> StavkeKorpe { get; set; } // Warning CS8618 ako nije inicijalizovan

        // Relacija 1 (Vozilo) -> 0..* (Recenzija)
        public ICollection<Recenzija> Recenzije { get; set; } // Warning CS8618 ako nije inicijalizovan
        // ========================================


        // Ažurirajte konstruktor da inicijalizuje kolekcije
        public Vozilo()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>(); // Inicijalizacija kolekcije
            Recenzije = new HashSet<Recenzija>(); // Inicijalizacija kolekcije

            // Također možete inicijalizovati non-nullable stringove da uklonite CS8618 warninge za njih, npr:
            // Marka = "";
            // Model = "";
            // Boja = "";
        }
    }
    // Entitet za Korpu (Shopping Cart)
    // U 1-na-1 relaciji sa ApplicationUser-om (PK Korpe je FK ka ApplicationUser-u)
    public class Korpa
    {
        [Key] // Obeležavamo KorpaID kao Primarni Ključ
        public int KorpaID { get; set; } // <-- DODAT NOVI INT PRIMARNI KLJUČ

        // KorisnikId je sada samo strani ključ (nije više PK) ka ApplicationUser-u
        // Mora biti Required jer svaka korpa mora pripadati nekom korisniku (prema 1-na-1 relaciji)
        // Mora biti Unique da bi se ispoštovala 1-na-1 relacija (konfiguriše se u DbContextu)
        [Required]
        public string KorisnikId { get; set; } // <-- OSTALO KAO STRANI KLJUČ KA ApplicationUser-u (string)

        // Navigaciono svojstvo ka ApplicationUser-u (strana "jedan" u 1-na-1 relaciji)
        public ApplicationUser Korisnik { get; set; }

        // Svojstvo za ukupnu cenu
        public decimal UkupnaCijena { get; set; }

        // Relacija 1 (Korpa) -> 0..* (StavkaKorpe)
        // Korpa sadrži mnogo StavkiKorpe. Strani ključ je KorpaID (int?) u StavkaKorpe.
        public ICollection<StavkaKorpe> StavkeKorpe { get; set; }

        public Korpa()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>();
        }
    }

    // Entitet za Stavku Korpe ili Stavku Narudžbe
    // Povezana i sa Korpa i sa Narudzba (prema dijagramu), i sa Vozilom
    public class StavkaKorpe // Možda bi bolji naziv bio Item (može biti CartItem ili OrderItem)
    {
        [Key]
        public int StavkaID { get; set; }

        [Required]
        public int Kolicina { get; set; }

        // Svojstvo za cenu stavke (može se izračunati ili snimiti)
        // Decimal je bolji za novčane vrednosti
        public decimal CijenaStavke { get; set; } // Cena pojedinačnog vozila * količina

        // --- Strani Ključevi ---

        [Required] // Stavka *mora* referencirati Vozilo
        public int VoziloID { get; set; }

        // Strani ključ ka Korpi (Nullable, jer stavka može biti u Korpi ILI u Narudzbi)
        public int? KorpaID { get; set; }

        // Strani ključ ka Narudzbi (Nullable, jer stavka može biti u Korpi ILI u Narudzbi)
        public int? NarudzbaID { get; set; }

        // --- Navigaciona svojstva ---

        // Relacija *..* (StavkaKorpe) -> 1 (Vozilo)
        public Vozilo Vozilo { get; set; }

        // Relacija *..* (StavkaKorpe) -> 1 (Korpa) (Nullable)
        public Korpa Korpa { get; set; }

        // Relacija *..* (StavkaKorpe) -> 1 (Narudzba) (Nullable)
        public Narudzba Narudzba { get; set; }

        // Nema potrebe za konstruktorom jer nema kolekcija unutar ove klase
    }

    // Entitet za Narudzbu (Order)
    public class Narudzba
    {
        [Key]
        public int NarudzbaID { get; set; }

        [Required]
        public DateTime DatumNarudzbe { get; set; }

        [Required] // Enum će se mapirati u string
        public StatusNarudzbe Status { get; set; }

        // Ukupna cena narudzbe (može se snimiti ili izračunavati)
        [Required]
        // Decimal je bolji za novčane vrednosti
        public decimal UkupnaCijena { get; set; }

        // --- Strani Ključ ---

        [Required] // Narudzba *mora* pripadati Korisniku
        public string KorisnikId { get; set; } // Koristi string Id tip iz ApplicationUser

        // --- Navigaciona svojstva ---

        // Relacija *..* (Narudzba) -> 1 (ApplicationUser)
        public ApplicationUser Korisnik { get; set; }

        // Relacija 1 (Narudzba) -> 1..* (StavkaKorpe)
        public ICollection<StavkaKorpe> StavkeKorpe { get; set; } // Sadrži stavke narudzbe

        // Relacija 1 (Narudzba) -> 1 (Placanje)
        public Placanje Placanje { get; set; } // Povezano placanje za ovu narudzbu

        public Narudzba()
        {
            StavkeKorpe = new HashSet<StavkaKorpe>();
            // Placanje se ne inicijalizuje kao kolekcija
        }
    }

    // Entitet za Placanje (Payment)
    // U 1-na-1 relaciji sa Narudzba (PK Plaćanja je FK ka Narudžbi)
    public class Placanje
    {
        // [Key] // Nije potrebno ako NarudzbaID služi kao PK (konfigurisano u DbContextu)
        // public int PlacanjeID { get; set; } // Standardni PK nije potreban u ovom 1-na-1 modelu

        [Key] // Konfigurisano u DbContextu da ovo bude i PK i FK
        public int NarudzbaID { get; set; }

        // Navigaciono svojstvo ka Narudzbi (strana "jedan" u 1-na-1 relaciji)
        public Narudzba Narudzba { get; set; }

        [Required]
        public DateTime DatumPlacanja { get; set; }

        [Required]
        // Decimal je bolji za novčane vrednosti
        public decimal Iznos { get; set; }

        [Required] // Enum će se mapirati u string
        public StatusPlacanja Status { get; set; }

        // Strategija (Verovatno interfejs ili enum van ovog modela) - ostavljeno kao komentar
        // public IPlacanjeStrategija Strategija { get; set; }

        // --- Strani Ključevi (Nullable za opcione veze) ---

        // Relacija 1 (Placanje) -> 0..1 (Kartica)
        public int? KarticaID { get; set; }

        // Relacija 1 (Placanje) -> 0..1 (Kredit)
        public int? KreditID { get; set; }

        // --- Navigaciona svojstva (Nullable za opcione veze) ---

        // Relacija *..* (Placanje) -> 1 (Kartica) (Nullable)
        public Kartica Kartica { get; set; }

        // Relacija *..* (Placanje) -> 1 (Kredit) (Nullable)
        public Kredit Kredit { get; set; }

        // Nema potrebe za konstruktorom
    }

    // Entitet za Karticu (Payment Card Info)
    public class Kartica
    {
        [Key]
        public int KarticaID { get; set; }

        [Required]
        [CreditCard] // Data anotacija za validaciju formata
        [MaxLength(20)] // Primer: maksimalna dužina broja kartice
        public string BrojKartice { get; set; }

        [Required]
        [MaxLength(5)] // MM/YY format
        public string DatumIsteka { get; set; } // Možda bolji tip bi bio DateTime ili zasebna svojstva

        [Required]
        [MaxLength(4)] // 3 ili 4 cifre
        public string Cvv { get; set; }

        [Required]
        [MaxLength(200)]
        public string ImeVlasnika { get; set; }

        // Validacija se obično radi u logici, ne u modelu entiteta direktno
        // +validirajKarticu() metoda bi bila u servisu ili logici

        // Nema potrebe za navigacionim svojstvima ka Placanjima ako nije potrebno navigirati unazad
    }

    // Entitet za Kredit (Instalment Plan Info)
    public class Kredit
    {
        [Key]
        public int KreditID { get; set; }

        [Required]
        public decimal Iznos { get; set; } // Decimal za novac

        [Required]
        public int BrojRata { get; set; }

        [Required]
        public decimal KamatnaStopa { get; set; } // Decimal za procente

        // Izračunata vrednost, ali može se i snimiti u bazi ako je potrebno
        public decimal MjesecnaRata { get; set; } // Decimal za novac

        // Logika izračunavanja bi bila u servisu ili metodi
        // +izracunajMjesecnuRatu()
        // +izracunajUkupniIznos()

        // Nema potrebe za navigacionim svojstvima ka Placanjima ako nije potrebno navigirati unazad
    }

    // Entitet za Recenziju (Review)
    public class Recenzija
    {
        [Key]
        public int RecenzijaID { get; set; }

        [Required]
        [Range(1, 5)] // Primer: ocena od 1 do 5
        public int Ocjena { get; set; }

        [MaxLength(1000)] // Primer: maksimalna dužina komentara
        public string Komentar { get; set; }

        [Required]
        public DateTime DatumRecenzije { get; set; }

        // --- Strani Ključevi ---

        [Required] // Recenzija *mora* biti povezana sa Korisnikom
        public string KorisnikId { get; set; } // Koristi string Id tip iz ApplicationUser

        [Required] // Recenzija *mora* biti povezana sa Vozilom
        public int VoziloID { get; set; }

        // --- Navigaciona svojstva ---

        // Relacija *..* (Recenzija) -> 1 (ApplicationUser)
        public ApplicationUser Korisnik { get; set; }

        // Relacija *..* (Recenzija) -> 1 (Vozilo)
        public Vozilo Vozilo { get; set; }

        // Nema potrebe za konstruktorom
    }

    // Entitet za Podršku (Support Ticket)
    public class Podrska
    {
        [Key] // Primarni ključ
        public int UpitID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Naslov { get; set; }

        [Required]
        public string Sadrzaj { get; set; } // Može biti duži tekst, bez MaxLength ili sa većim brojem

        [Required]
        public DateTime DatumUpita { get; set; }

        [Required] // Enum će se mapirati u string
        public StatusUpita Status { get; set; }

        // --- Strani Ključ ---

        [Required] // Upit *mora* biti povezan sa Korisnikom
        public string KorisnikId { get; set; } // Koristi string Id tip iz ApplicationUser

        // --- Navigaciono svojstvo ---

        // Relacija *..* (Podrska) -> 1 (ApplicationUser)
        public ApplicationUser Korisnik { get; set; }

        // Nema potrebe za konstruktorom
    }
}