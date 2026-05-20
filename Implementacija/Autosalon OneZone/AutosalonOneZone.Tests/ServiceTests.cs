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
        yield return [null, null, null, null, null, 40000m, null, 4];
        yield return [null, null, null, null, null, null, 60000m, 2];
        yield return ["Audi", null, 2021, null, null, 100000m, null, 1];
    }

    [Theory]
    [MemberData(nameof(FilterCases))]
    public async Task VoziloService_filters_vehicles(string marka, string model, int? yearFrom, int? yearTo, TipGoriva? fuel, decimal? priceFrom, decimal? priceTo, int expected)
    {
        await using var db = CreateContext();
        await SeedVehicles(db);
        var service = new VoziloService(db);

        var vehicles = await service.FilterVozilaAsync(marka, model, yearFrom, yearTo, fuel, priceFrom, priceTo);

        Assert.Equal(expected, vehicles.Count());
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
            CreateVehicle("Audi", "A4", 2020, 43900, TipGoriva.Dizel, 76000),
            CreateVehicle("Audi", "e-tron GT", 2022, 129500, TipGoriva.Elektro, 28400),
            CreateVehicle("BMW", "X5", 2022, 89500, TipGoriva.Dizel, 38500),
            CreateVehicle("Tesla", "Model 3", 2023, 72800, TipGoriva.Elektro, 21400),
            CreateVehicle("Volkswagen", "Golf 8", 2021, 37500, TipGoriva.Benzin, 48500));
        await db.SaveChangesAsync();
    }

    private static Vozilo CreateVehicle(string brand, string model, int year, decimal price, TipGoriva fuel = TipGoriva.Benzin, double mileage = 1000)
    {
        return new Vozilo
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = "Crna",
            Kilometraza = mileage,
            Cijena = price,
            Opis = "Opis"
        };
    }
}
