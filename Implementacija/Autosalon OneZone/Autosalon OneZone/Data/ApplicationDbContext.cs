// Put this file in your Data or Persistence folder, e.g., Autosalon_OneZone/Data/ApplicationDbContext.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Autosalon_OneZone.Models; // Uvjerite se da je ovo ispravan namespace za vaše modele

namespace Autosalon_OneZone.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

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
            base.OnModelCreating(modelBuilder);

            // --- Mapiranje Enumeracija ---
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
                .HasForeignKey(n => n.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade); // Kaskadno brisanje narudžbi kada se korisnik obriše.

            // ApplicationUser (1) -> Korpa (0..1)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Korpa)
                .WithOne(k => k.Korisnik)
                .HasForeignKey<Korpa>(k => k.KorisnikId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Kaskadno brisanje korpe kada se korisnik obriše.

            modelBuilder.Entity<Korpa>()
                .HasIndex(k => k.KorisnikId)
                .IsUnique();

            // ApplicationUser (1) -> Recenzija (0..*)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Recenzije)
                .WithOne(r => r.Korisnik)
                .HasForeignKey(r => r.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser (1) -> Podrska (0..*)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.PodrskaUpiti)
                .WithOne(p => p.Korisnik)
                .HasForeignKey(p => p.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade); // Kaskadno brisanje upita podrške kada se korisnik obriše.

            // Vozilo (1) -> StavkaKorpe (0..*)
            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.StavkeKorpe)
                .WithOne(s => s.Vozilo)
                .HasForeignKey(s => s.VoziloID)
                .OnDelete(DeleteBehavior.Cascade); // Kaskadno brisanje stavki korpe kada se vozilo obriše.

            // Vozilo (1) -> Recenzija (0..*)
            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.Recenzije)
                .WithOne(r => r.Vozilo)
                .HasForeignKey(r => r.VoziloID)
                .OnDelete(DeleteBehavior.Cascade);

            // Korpa (1) -> StavkaKorpe (0..*)
            modelBuilder.Entity<Korpa>()
                .HasMany(k => k.StavkeKorpe)
                .WithOne(s => s.Korpa)
                .HasForeignKey(s => s.KorpaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade); // Kaskadno brisanje stavki korpe kada se korpa obriše.

            // Narudzba (1) -> StavkaKorpe (1..*)
            // IZMIJENJENO: Postavljeno na DeleteBehavior.Restrict da se izbjegne problem "multiple cascade paths".
            // Ako se Narudzba obriše, stavke korpe neće biti automatski obrisane ako su dio i Korpe i Narudzbe.
            // Baza podataka neće dozvoliti kaskadno brisanje kroz dva puta do iste tabele.
            modelBuilder.Entity<Narudzba>()
                .HasMany(n => n.StavkeKorpe)
                .WithOne(s => s.Narudzba)
                .HasForeignKey(s => s.NarudzbaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Narudzba (1) -> Placanje (1)
            modelBuilder.Entity<Narudzba>()
                .HasOne(n => n.Placanje)
                .WithOne(p => p.Narudzba)
                .HasForeignKey<Placanje>(p => p.NarudzbaID)
                .IsRequired();

            // Placanje (1) -> Kartica (0..1)
            modelBuilder.Entity<Placanje>()
                .HasOne(p => p.Kartica)
                .WithMany()
                .HasForeignKey(p => p.KarticaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Placanje (1) -> Kredit (0..1)
            modelBuilder.Entity<Placanje>()
                .HasOne(p => p.Kredit)
                .WithMany()
                .HasForeignKey(p => p.KreditID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);


            // --- Konfiguracija Svojstava (sa rješenjima za decimal upozorenja) ---

            modelBuilder.Entity<Vozilo>()
                .Property(v => v.Cijena)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Korpa>()
                .Property(k => k.UkupnaCijena)
                .HasColumnType("decimal(18,2)"); // DODANO: Rješava upozorenje za Korpa.UkupnaCijena

            modelBuilder.Entity<Narudzba>()
                .Property(n => n.UkupnaCijena)
                .HasColumnType("decimal(18,2)"); // DODANO: Rješava upozorenje za Narudzba.UkupnaCijena

            modelBuilder.Entity<Placanje>()
                .HasKey(p => p.NarudzbaID); // Ostaje kao PK

            modelBuilder.Entity<Placanje>()
                .Property(p => p.Iznos)
                .HasColumnType("decimal(18,2)"); // DODANO: Rješava upozorenje za Placanje.Iznos

            modelBuilder.Entity<StavkaKorpe>()
                .Property(s => s.Kolicina)
                .IsRequired();
            modelBuilder.Entity<StavkaKorpe>()
                .Property(s => s.CijenaStavke)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Recenzija>()
                .Property(r => r.Ocjena)
                .IsRequired();
            modelBuilder.Entity<Recenzija>()
                .Property(r => r.Komentar)
                .HasMaxLength(1000);

            modelBuilder.Entity<Podrska>()
                .Property(p => p.Naslov)
                .IsRequired()
                .HasMaxLength(200);
            modelBuilder.Entity<Podrska>()
                .Property(p => p.Sadrzaj)
                .IsRequired();

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

            modelBuilder.Entity<Kredit>()
                .Property(cr => cr.Iznos)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Kredit>()
               .Property(cr => cr.KamatnaStopa)
               .IsRequired()
               .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Kredit>()
               .Property(cr => cr.MjesecnaRata)
               .HasColumnType("decimal(18,2)");
        }
    }
}