using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Autosalon_OneZone.Models;

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
        public DbSet<PorukaPodrske> PorukePodrske { get; set; }
        public DbSet<EmailPodrskeOutbox> EmailPodrskeOutbox { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vozilo>()
               .Property(v => v.Kubikaza)
               .HasColumnType("decimal(18,1)");

            modelBuilder.Entity<Vozilo>()
                .Property(e => e.Gorivo)
                .HasConversion<string>();

            modelBuilder.Entity<Vozilo>()
                .Property(e => e.Boja)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Narudzba>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Placanje>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Podrska>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<PorukaPodrske>()
                .Property(e => e.TipAutora)
                .HasConversion<string>();

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Narudzbe)
                .WithOne(n => n.Korisnik)
                .HasForeignKey(n => n.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Korpa)
                .WithOne(k => k.Korisnik)
                .HasForeignKey<Korpa>(k => k.KorisnikId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Korpa>()
                .HasIndex(k => k.KorisnikId)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Recenzije)
                .WithOne(r => r.Korisnik)
                .HasForeignKey(r => r.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.PodrskaUpiti)
                .WithOne(p => p.Korisnik)
                .HasForeignKey(p => p.KorisnikId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.DodijeljeniUpiti)
                .WithOne(p => p.DodijeljenKorisnik)
                .HasForeignKey(p => p.DodijeljenKorisnikId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.PorukePodrske)
                .WithOne(p => p.Posiljalac)
                .HasForeignKey(p => p.PosiljalacId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Podrska>()
                .HasMany(p => p.Poruke)
                .WithOne(p => p.Upit)
                .HasForeignKey(p => p.UpitID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PorukaPodrske>()
                .HasOne(p => p.EmailOutbox)
                .WithOne(e => e.Poruka)
                .HasForeignKey<EmailPodrskeOutbox>(e => e.PorukaPodrskeID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.StavkeKorpe)
                .WithOne(s => s.Vozilo)
                .HasForeignKey(s => s.VoziloID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vozilo>()
                .HasMany(v => v.Recenzije)
                .WithOne(r => r.Vozilo)
                .HasForeignKey(r => r.VoziloID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Korpa>()
                .HasMany(k => k.StavkeKorpe)
                .WithOne(s => s.Korpa)
                .HasForeignKey(s => s.KorpaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Narudzba>()
                .HasMany(n => n.StavkeKorpe)
                .WithOne(s => s.Narudzba)
                .HasForeignKey(s => s.NarudzbaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Narudzba>()
                .HasOne(n => n.Placanje)
                .WithOne(p => p.Narudzba)
                .HasForeignKey<Placanje>(p => p.NarudzbaID)
                .IsRequired();

            modelBuilder.Entity<Placanje>()
                .HasOne(p => p.Kartica)
                .WithMany()
                .HasForeignKey(p => p.KarticaID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Placanje>()
                .HasOne(p => p.Kredit)
                .WithMany()
                .HasForeignKey(p => p.KreditID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Korpa>()
                .Property(k => k.UkupnaCijena)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Narudzba>()
                .Property(n => n.UkupnaCijena)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Placanje>()
                .HasKey(p => p.NarudzbaID);

            modelBuilder.Entity<Placanje>()
                .Property(p => p.Iznos)
                .HasColumnType("decimal(18,2)");

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
                .Property(p => p.Jezik)
                .IsRequired()
                .HasMaxLength(10);
            var supportRowVersion = modelBuilder.Entity<Podrska>()
                .Property(p => p.RowVersion)
                .IsRowVersion();
            if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                supportRowVersion.ValueGeneratedNever();
            }
            modelBuilder.Entity<Podrska>()
                .HasIndex(p => new { p.Status, p.DatumZadnjeAktivnosti });
            modelBuilder.Entity<Podrska>()
                .HasIndex(p => p.DodijeljenKorisnikId);

            modelBuilder.Entity<PorukaPodrske>()
                .Property(p => p.Sadrzaj)
                .IsRequired()
                .HasMaxLength(5000);
            modelBuilder.Entity<PorukaPodrske>()
                .HasIndex(p => new { p.UpitID, p.DatumSlanja });
            modelBuilder.Entity<PorukaPodrske>()
                .HasIndex(p => new { p.UpitID, p.TipAutora, p.ProcitanaUtc });

            modelBuilder.Entity<EmailPodrskeOutbox>()
                .HasIndex(e => new { e.PoslanoUtc, e.SljedeciPokusajUtc });

            modelBuilder.Entity<Kartica>()
                .Property(c => c.BrojKartice)
                .IsRequired()
                .HasMaxLength(20);
            modelBuilder.Entity<Kartica>()
                .Property(c => c.DatumIsteka)
                .IsRequired()
                .HasMaxLength(5);
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
