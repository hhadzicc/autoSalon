using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutosalonOneZone.Tests;

public class IntegrationSmokeTests : IClassFixture<AutosalonFactory>
{
    private readonly HttpClient _client;

    public IntegrationSmokeTests(AutosalonFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    [InlineData("/Vozilo")]
    [InlineData("/Vozilo/Details/1")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Home/Kontakt")]
    [InlineData("/Home/Privacy")]
    public async Task Public_pages_return_success(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.True(response.IsSuccessStatusCode, $"{path} returned {(int)response.StatusCode}");
    }

    [Theory]
    [InlineData("/Profil")]
    [InlineData("/Profil/Edit")]
    [InlineData("/Profil/KupljeniArtikli")]
    [InlineData("/Korpa")]
    [InlineData("/AdminPanel")]
    public async Task Protected_pages_redirect_or_forbid_anonymous_users(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.Found ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden,
            $"{path} returned {(int)response.StatusCode}");
    }

    [Theory]
    [InlineData("/Account/Login", "Autosalon OneZone")]
    [InlineData("/Account/Register", "Autosalon OneZone")]
    [InlineData("/Vozilo", "Vehicles")]
    [InlineData("/Home/Kontakt", "Contact")]
    public async Task Public_pages_render_expected_text(string path, string expected)
    {
        var html = await _client.GetStringAsync(path);

        Assert.Contains(expected, html, StringComparison.OrdinalIgnoreCase);
    }
}

public class AutosalonFactory : WebApplicationFactory<Autosalon_OneZone.Controllers.HomeController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var databaseName = $"autosalon-tests-{Guid.NewGuid():N}";

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IPaymentService>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            services.AddSingleton<IPaymentService>(_ => new MockPaymentService(NullLogger<MockPaymentService>.Instance));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            Seed(db);
        });
    }

    private static void Seed(ApplicationDbContext db)
    {
        if (db.Vozila.Any())
        {
            return;
        }

        db.Vozila.AddRange(
            new Vozilo
            {
                VoziloID = 1,
                Marka = "Audi",
                Model = "A4",
                Godiste = 2020,
                Gorivo = TipGoriva.Dizel,
                Kubikaza = 2.0m,
                Boja = "Bijela",
                Kilometraza = 76000,
                Cijena = 43900,
                Slika = "seed-audi-a4-2020-showroom.webp",
                Opis = "Test vozilo"
            },
            new Vozilo
            {
                VoziloID = 2,
                Marka = "Tesla",
                Model = "Model 3",
                Godiste = 2023,
                Gorivo = TipGoriva.Elektro,
                Kubikaza = 1.0m,
                Boja = "Crna",
                Kilometraza = 21400,
                Cijena = 72800,
                Slika = "seed-tesla-model3-2023-showroom.webp",
                Opis = "Test vozilo"
            });

        db.SaveChanges();
    }
}
