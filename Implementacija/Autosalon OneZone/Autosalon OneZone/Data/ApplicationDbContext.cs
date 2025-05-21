// Put this file in your Data or Persistence folder, e.g., Autosalon_OneZone/Data/ApplicationDbContext.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Potrebno za IdentityDbContext
using Microsoft.AspNetCore.Identity; // Potrebno za IdentityRole ako je koristite direktno
using System;
using System.Collections.Generic;

using Autosalon_OneZone.Models;

// DODAJTE OVAJ NAMESPACE BLOK
namespace Autosalon_OneZone.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string> // Dodali IdentityRole i string za tip PK (ApplicationUser)
    {
        // Konstruktor ostaje isti
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet za ApplicationUser i IdentityRole se automatski dodaje nasleđivanjem IdentityDbContext
        // public DbSet<ApplicationUser> ApplicationUsers { get; set; } // Opciono, ali nije neophodno
        // public DbSet<IdentityRole> Roles { get; set; } // Opciono, ali nije neophodno

        // DbSet svojstva za ostale entitete
        public DbSet<Vozilo> Vozila { get; set; }
        public DbSet<Korpa> Korpe { get; set; }
        public DbSet<StavkaKorpe> StavkeKorpe { get; set; }
        public DbSet<Narudzba> Narudzbe { get; set; }
        public DbSet<Placanje> Placanja { get; set; }
        public DbSet<Kartica> Kartice { get; set; }
        public DbSet<Kredit> Krediti { get; set; }
        public DbSet<Recenzija> Recenzije { get; set; }
        public DbSet<Podrska> PodrskaUpiti { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BITNO: Morate pozvati base.OnModelCreating pre vaše konfiguracije
            // kako bi Identity model bio ispravno konfigurisan
            base.OnModelCreating(modelBuilder);

            // Promenite imena Identity tabela ako želite (opciono)
            // Example: modelBuilder.Entity<ApplicationUser>().ToTable("AppUsers");
            // Example: modelBuilder.Entity<IdentityRole>().ToTable("AppRoles");
            // Note: Renaming Identity tables requires renaming join tables as well (e.g., AspNetUserRoles)

            // --- Mapiranje Enumeracija ---
            // VrstaRacuna mapiranje za staru klasu Korisnik više NIJE relevantno
            // Role se upravljaju kroz IdentityRole
            // modelBuilder.Entity<Korisnik>().Property(e => e.VrstaRacuna).HasConversion<string>(); // UKLONJENO
            modelBuilder.Entity<Vozilo>()
           .Property(v => v.Kubikaza)
           .HasColumnType("decimal(18,1)");


            modelBuilder.Entity<Vozilo>()
                .Property(e => e.Gorivo)
                .HasConversion<string>();

            modelBuilder.Entity<Narudzba>()
                 .Property(e => e.Status)
                 .HasConversion<string>();

            modelBuilder.Entity<Placanje>()
                 .Property(e => e.Status)
                 .HasConversion<string>();

            modelBuilder.Entity<Podrska>()
                 .Property(e => e.Status)
                 .HasConversion<string>();


            // --- Konfiguracija Relacija ---

            // ApplicationUser (1) -> Narudzba (0..*)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Narudzbe)
                .WithOne(n => n.Korisnik)
                .HasForeignKey(n => n.KorisnikId) // FK KorisnikId (string) u Narudzba
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser (1) -> Korpa (0..1)
            // ApplicationUser je principal (1), Korpa je dependent (0..1)
            // Strani ključ je KorisnikId (string) u Korpa tabeli. Mora biti Unique.
            modelBuilder.Entity<ApplicationUser>()
                 .HasOne(u => u.Korpa) // ApplicationUser ima jednu Korpu
                 .WithOne(k => k.Korisnik) // Korpa pripada jednom ApplicationUseru
                 .HasForeignKey<Korpa>(k => k.KorisnikId) // Strani ključ KorisnikId (string) u tabeli Korpa
                 .IsRequired(); // Svaka Korpa mora imati povezanog Korisnika

            // !!! Važno: Strani ključ KorisnikId u Korpa tabeli MORA biti Unique da bi se osigurala 1-na-1 veza !!!
            modelBuilder.Entity<Korpa>()
                .HasIndex(k => k.KorisnikId)
                .IsUnique(); // Dodajemo Unique Index na strani ključ ka korisniku

            // ApplicationUser (1) -> Recenzija (0..*)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Recenzije)
                .WithOne(r => r.Korisnik)
                .HasForeignKey(r => r.KorisnikId) // FK KorisnikId (string) u Recenzija
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser (1) -> Podrska (0..*)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.PodrskaUpiti)
                .WithOne(p => p.Korisnik)
                .HasForeignKey(p => p.KorisnikId) // FK KorisnikId (string) u Podrska
                .OnDelete(DeleteBehavior.Restrict);

            // Vozilo (1) -> StavkaKorpe (0..*)
            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.StavkeKorpe)
                .WithOne(s => s.Vozilo)
                .HasForeignKey(s => s.VoziloID) // FK VoziloID (int) u StavkaKorpe
                .OnDelete(DeleteBehavior.Restrict);

            // Vozilo (1) -> Recenzija (0..*)
            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.Recenzije)
                .WithOne(r => r.Vozilo)
                .HasForeignKey(r => r.VoziloID) // FK VoziloID (int) u Recenzija
                .OnDelete(DeleteBehavior.Cascade);

            // Korpa (1) -> StavkaKorpe (0..*)
            // Strani ključ KorpaID (int?) u StavkaKorpe referencira Primarni Ključ KorpaID (int) u Korpa. Tipovi se slažu.
            modelBuilder.Entity<Korpa>()
                .HasMany(k => k.StavkeKorpe)
                .WithOne(s => s.Korpa)
                .HasForeignKey(s => s.KorpaID) // FK KorpaID (int?) u StavkaKorpe
                .IsRequired(false) // Strani ključ je nullable
                .OnDelete(DeleteBehavior.Cascade);

            // Narudzba (1) -> StavkaKorpe (1..*)
            // Strani ključ NarudzbaID (int?) u StavkaKorpe referencira Primarni Ključ NarudzbaID (int) u Narudzba. Tipovi se slažu.
            modelBuilder.Entity<Narudzba>()
                .HasMany(n => n.StavkeKorpe)
                .WithOne(s => s.Narudzba)
                .HasForeignKey(s => s.NarudzbaID) // FK NarudzbaID (int?) u StavkaKorpe
                .IsRequired(false) // Strani ključ je nullable (jer stavka moze biti i u Korpi)
                .OnDelete(DeleteBehavior.Cascade);

            // Narudzba (1) -> Placanje (1)
            // Strani ključ NarudzbaID je PK Placanja.
            modelBuilder.Entity<Narudzba>()
                 .HasOne(n => n.Placanje)
                 .WithOne(p => p.Narudzba)
                 .HasForeignKey<Placanje>(p => p.NarudzbaID) // Strani ključ NarudzbaID (int) je PK Placanja
                 .IsRequired();

            // Placanje (1) -> Kartica (0..1)
            modelBuilder.Entity<Placanje>()
                 .HasOne(p => p.Kartica)
                 .WithMany() // Kartica nema navigaciono svojstvo nazad ka Placanjima
                 .HasForeignKey(p => p.KarticaID) // FK KarticaID (int?) u Placanje
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);

            // Placanje (1) -> Kredit (0..1)
            modelBuilder.Entity<Placanje>()
                 .HasOne(p => p.Kredit)
                 .WithMany() // Kredit nema navigaciono svojstvo nazad ka Placanjima
                 .HasForeignKey(p => p.KreditID) // FK KreditID (int?) u Placanje
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.Restrict);


            // --- Konfiguracija Svojstava ---
            // Konfiguracija emaila je sada u Identity sistemu

            modelBuilder.Entity<Vozilo>()
                 .Property(v => v.Cijena)
                 .HasColumnType("decimal(18,2)");

            // Primarni ključ KorpaID (int) u Korpa se postavlja konvencijom
            // Više NE treba modelBuilder.Entity<Korpa>().HasKey(k => k.KorisnikId);

            // Primarni ključ Plaćanja je i dalje NarudzbaID (int)
            modelBuilder.Entity<Placanje>().HasKey(p => p.NarudzbaID);

            // Konfiguracija za StavkaKorpe svojstva
            modelBuilder.Entity<StavkaKorpe>()
                 .Property(s => s.Kolicina)
                 .IsRequired();
            modelBuilder.Entity<StavkaKorpe>()
                .Property(s => s.CijenaStavke)
                .HasColumnType("decimal(18,2)");


            // Svojstva poput Ocjena i Komentar u Recenzija
            modelBuilder.Entity<Recenzija>()
                .Property(r => r.Ocjena)
                .IsRequired();
            modelBuilder.Entity<Recenzija>()
                .Property(r => r.Komentar)
                .HasMaxLength(1000);


            // Svojstva poput Naslov i Sadrzaj u Podrska
            modelBuilder.Entity<Podrska>()
                .Property(p => p.Naslov)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Podrska>()
                .Property(p => p.Sadrzaj)
                .IsRequired();


            // Konfiguracija za Kartica svojstva
            modelBuilder.Entity<Kartica>()
                .Property(c => c.BrojKartice)
                .IsRequired()
                .HasMaxLength(20);
            modelBuilder.Entity<Kartica>()
                .Property(c => c.DatumIsteka)
                .IsRequired()
                .HasMaxLength(5); // MM/YY
            modelBuilder.Entity<Kartica>()
                .Property(c => c.Cvv)
                .IsRequired()
                .HasMaxLength(4);
            modelBuilder.Entity<Kartica>()
                .Property(c => c.ImeVlasnika)
                .IsRequired()
                .HasMaxLength(200);

            // Konfiguracija za Kredit svojstva
            modelBuilder.Entity<Kredit>()
                .Property(cr => cr.Iznos)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Kredit>()
               .Property(cr => cr.KamatnaStopa)
               .IsRequired()
               .HasColumnType("decimal(18,2)"); // Podesite preciznost ako je potrebno
            modelBuilder.Entity<Kredit>()
               .Property(cr => cr.MjesecnaRata)
               .HasColumnType("decimal(18,2)");
        }
    }
}