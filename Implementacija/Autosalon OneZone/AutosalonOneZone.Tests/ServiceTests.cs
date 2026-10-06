#nullable disable

using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutosalonOneZone.Tests;

public class ServiceTests
{
    [Theory]
    [InlineData(100, "4242424242424242", true)]
    [InlineData(100, "4242 4242 4242 4242", true)]
    [InlineData(100, "4000000000000002", false)]
    [InlineData(0, "4242424242424242", false)]
    [InlineData(-10, "4242424242424242", false)]
    [InlineData(100, "123", false)]
    [InlineData(100, "123456789012345678901", false)]
    [InlineData(50000, "5555555555554444", true)]
    public async Task MockPaymentService_returns_expected_result(decimal amount, string card, bool expected)
    {
        var service = new MockPaymentService(NullLogger<MockPaymentService>.Instance);

        var result = await service.ProcessPaymentAsync(new PaymentRequest
        {
            Amount = amount,
            CardNumber = card,
            CustomerName = "Test User",
            Email = "test@example.com",
            Description = "Test",
            ProductId = 1
        });

        Assert.Equal(expected, result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.TransactionId));
    }

    [Fact]
    public async Task VoziloService_adds_and_gets_vehicle()
    {
        await using var db = CreateContext();
        var service = new VoziloService(db);

        var added = await service.AddVoziloAsync(CreateVehicle("Audi", "A4", 2020, 43900));
        var loaded = await service.GetVoziloByIdAsync(added.VoziloID);

        Assert.NotNull(loaded);
        Assert.Equal("Audi", loaded.Marka);
        Assert.Equal("A4", loaded.Model);
    }

    [Fact]
    public async Task VoziloService_returns_all_vehicles()
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);

        var vehicles = await service.GetAllVozilaAsync();

        Assert.Equal(5, vehicles.Count());
    }

    [Theory]
    [InlineData("Audi", 2)]
    [InlineData("BMW", 1)]
    [InlineData("Tesla", 1)]
    [InlineData("Missing", 0)]
    public async Task VoziloService_searches_by_brand_or_model(string term, int expected)
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);

        var vehicles = await service.SearchVozilaAsync(term);

        Assert.Equal(expected, vehicles.Count());
    }

    [Fact]
    public async Task VoziloService_search_empty_returns_empty_list()
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);

        var vehicles = await service.SearchVozilaAsync("");

        Assert.Empty(vehicles);
    }

    [Fact]
    public async Task VoziloService_updates_existing_vehicle()
    {
        await using var db = CreateContext();
        var existing = await new VoziloService(db).AddVoziloAsync(CreateVehicle("Audi", "A4", 2020, 43900));
        var service = new VoziloService(db);

        existing.Cijena = 45000;
        existing.Model = "A4 Avant";
        var updated = await service.UpdateVoziloAsync(existing);

        Assert.NotNull(updated);
        Assert.Equal("A4 Avant", updated.Model);
        Assert.Equal(45000, updated.Cijena);
    }

    [Fact]
    public async Task VoziloService_update_missing_returns_null()
    {
        await using var db = CreateContext();
        var service = new VoziloService(db);

        var updated = await service.UpdateVoziloAsync(CreateVehicle("Missing", "Car", 2021, 1));

        Assert.Null(updated);
    }

    [Fact]
    public async Task VoziloService_deletes_existing_vehicle()
    {
        await using var db = CreateContext();
        var service = new VoziloService(db);
        var added = await service.AddVoziloAsync(CreateVehicle("Audi", "A4", 2020, 43900));

        var deleted = await service.DeleteVoziloAsync(added.VoziloID);

        Assert.True(deleted);
        Assert.Null(await service.GetVoziloByIdAsync(added.VoziloID));
    }

    [Fact]
    public async Task VoziloService_delete_missing_returns_false()
    {
        await using var db = CreateContext();
        var service = new VoziloService(db);

        Assert.False(await service.DeleteVoziloAsync(999));
    }

    public static IEnumerable<object[]> FilterCases()
    {
        yield return ["Audi", null, null, null, null, null, null, 2];
        yield return [null, "Model", null, null, null, null, null, 1];
        yield return [null, null, 2021, null, null, null, null, 4];
        yield return [null, null, null, 2021, null, null, null, 2];
        yield return [null, null, null, null, TipGoriva.Elektro, null, null, 2];
        yield return [null, null, null, null, null, 40000, null, 4];
        yield return [null, null, null, null, null, null, 60000, 2];
        yield return ["Audi", null, 2021, null, null, 100000, null, 1];
    }

    [Theory]
    [MemberData(nameof(FilterCases))]
    public async Task VoziloService_filters_vehicles(string marka, string model, int? yearFrom, int? yearTo, TipGoriva? fuel, int? priceFrom, int? priceTo, int expected)
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);

        var vehicles = await service.FilterVozilaAsync(marka, model, yearFrom, yearTo, fuel, priceFrom, priceTo);

        Assert.Equal(expected, vehicles.Count());
    }

    [Fact]
    public async Task VoziloService_filters_catalog_by_multiple_exact_colors()
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);
        var criteria = new VehicleSearchCriteria(
            null,
            null,
            null,
            null,
            null,
            new[] { TipBoje.Crna, TipBoje.Srebrna },
            null,
            null,
            null,
            null,
            null,
            null);

        var result = await service.GetVehiclesPageAsync(criteria, 1, 12);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Vehicles, vehicle =>
            Assert.Contains(vehicle.Boja, new[] { TipBoje.Crna, TipBoje.Srebrna }));
    }

    [Fact]
    public async Task Customer_experience_summary_contains_only_completed_purchases_and_orders_latest_first()
    {
        await using var db = CreateContext();
        var buyer = new ApplicationUser { Id = "buyer", Ime = "Demo", Prezime = "Kupac", UserName = "demo" };
        var pendingBuyer = new ApplicationUser { Id = "pending", Ime = "Novi", Prezime = "Kupac", UserName = "novi" };
        var firstVehicle = CreateVehicle("Lexus", "LC 500", 2021, 109900);
        var secondVehicle = CreateVehicle("Porsche", "Taycan 4S", 2021, 103900, TipGoriva.Elektro);
        var pendingVehicle = CreateVehicle("Audi", "A8", 2022, 120000);
        db.AddRange(buyer, pendingBuyer, firstVehicle, secondVehicle, pendingVehicle);
        await db.SaveChangesAsync();

        var firstOrder = new Narudzba { KorisnikId = buyer.Id, DatumNarudzbe = DateTime.UtcNow.AddDays(-20), Status = StatusNarudzbe.Placena, UkupnaCijena = 109900 };
        var secondOrder = new Narudzba { KorisnikId = buyer.Id, DatumNarudzbe = DateTime.UtcNow.AddDays(-10), Status = StatusNarudzbe.Isporucena, UkupnaCijena = 103900 };
        var pendingOrder = new Narudzba { KorisnikId = pendingBuyer.Id, DatumNarudzbe = DateTime.UtcNow.AddDays(-5), Status = StatusNarudzbe.Kreirana, UkupnaCijena = 120000 };
        db.AddRange(firstOrder, secondOrder, pendingOrder);
        await db.SaveChangesAsync();

        db.StavkeKorpe.AddRange(
            new StavkaKorpe { NarudzbaID = firstOrder.NarudzbaID, VoziloID = firstVehicle.VoziloID, Kolicina = 1, CijenaStavke = 109900 },
            new StavkaKorpe { NarudzbaID = secondOrder.NarudzbaID, VoziloID = secondVehicle.VoziloID, Kolicina = 1, CijenaStavke = 103900 },
            new StavkaKorpe { NarudzbaID = pendingOrder.NarudzbaID, VoziloID = pendingVehicle.VoziloID, Kolicina = 1, CijenaStavke = 120000 });
        db.Recenzije.AddRange(
            new Recenzija { KorisnikId = buyer.Id, VoziloID = firstVehicle.VoziloID, Ocjena = 4, Komentar = "Odlično iskustvo.", DatumRecenzije = DateTime.UtcNow.AddDays(-8) },
            new Recenzija { KorisnikId = buyer.Id, VoziloID = secondVehicle.VoziloID, Ocjena = 5, Komentar = "Sve preporuke.", DatumRecenzije = DateTime.UtcNow.AddDays(-2) },
            new Recenzija { KorisnikId = pendingBuyer.Id, VoziloID = pendingVehicle.VoziloID, Ocjena = 1, Komentar = "Kupovina nije završena.", DatumRecenzije = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        var summary = await new VoziloService(db).GetCustomerExperienceSummaryAsync(2);

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(4.5, summary.AverageRating);
        Assert.Collection(
            summary.Recent,
            experience => Assert.Equal("Porsche Taycan 4S", experience.VehicleName),
            experience => Assert.Equal("Lexus LC 500", experience.VehicleName));
    }

    [Fact]
    public async Task Purchase_experience_can_be_added_only_after_completed_order()
    {
        await using var db = CreateContext();
        var buyer = new ApplicationUser { Id = "buyer", Ime = "Demo", Prezime = "Kupac", UserName = "demo" };
        var vehicle = CreateVehicle("Genesis", "G80", 2022, 69900);
        db.AddRange(buyer, vehicle);
        await db.SaveChangesAsync();

        var order = new Narudzba { KorisnikId = buyer.Id, DatumNarudzbe = DateTime.UtcNow, Status = StatusNarudzbe.Kreirana, UkupnaCijena = 69900 };
        db.Narudzbe.Add(order);
        await db.SaveChangesAsync();
        db.StavkeKorpe.Add(new StavkaKorpe { NarudzbaID = order.NarudzbaID, VoziloID = vehicle.VoziloID, Kolicina = 1, CijenaStavke = 69900 });
        await db.SaveChangesAsync();

        var service = new ProfileActivityService(db);
        Assert.Equal(ReviewSaveResult.NotPurchased, await service.SaveReviewAsync(buyer.Id, vehicle.VoziloID, 5, "Prerano."));

        order.Status = StatusNarudzbe.Placena;
        await db.SaveChangesAsync();

        Assert.Equal(ReviewSaveResult.Added, await service.SaveReviewAsync(buyer.Id, vehicle.VoziloID, 5, "Odlično iskustvo kupovine."));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedVehicles(ApplicationDbContext db)
    {
        db.Vozila.AddRange(
            CreateVehicle("Audi", "A4", 2020, 43900, TipGoriva.Dizel, 76000, TipBoje.Crna),
            CreateVehicle("Audi", "e-tron GT", 2022, 129500, TipGoriva.Elektro, 28400, TipBoje.Bijela),
            CreateVehicle("BMW", "X5", 2022, 89500, TipGoriva.Dizel, 38500, TipBoje.Srebrna),
            CreateVehicle("Tesla", "Model 3", 2023, 72800, TipGoriva.Elektro, 21400, TipBoje.Crvena),
            CreateVehicle("Volkswagen", "Golf 8", 2021, 37500, TipGoriva.Benzin, 48500, TipBoje.Plava));
        await db.SaveChangesAsync();
    }

    private static Vozilo CreateVehicle(
        string brand,
        string model,
        int year,
        int price,
        TipGoriva fuel = TipGoriva.Benzin,
        int mileage = 1000,
        TipBoje color = TipBoje.Crna)
    {
        return new Vozilo
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = color,
            Kilometraza = mileage,
            Cijena = price,
            Opis = "Opis"
        };
    }
}
