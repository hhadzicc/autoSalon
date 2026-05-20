#nullable disable

using System.Security.Claims;
using System.Text.Json;
using Autosalon_OneZone.Controllers;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Autosalon_OneZone.Services;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;

namespace AutosalonOneZone.Tests;

public class AdminLifecycleTests
{
    [Fact]
    public async Task Admin_user_crud_can_create_edit_reset_password_and_delete_with_related_data()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        var createResult = await admin.SaveProfil(new AddProfilViewModel
        {
            UserName = "kupac1",
            Email = "kupac1@example.com",
            Ime = "Demo",
            Prezime = "Kupac",
            Password = "Valid123",
            ConfirmPassword = "Valid123",
            OdabraneRole = ["Kupac"]
        });

        Assert.IsType<OkObjectResult>(createResult);

        var user = await app.UserManager.FindByNameAsync("kupac1");
        Assert.NotNull(user);
        Assert.True(await app.UserManager.CheckPasswordAsync(user, "Valid123"));
        Assert.Contains("Kupac", await app.UserManager.GetRolesAsync(user));

        var editResult = await admin.SaveProfil(new AddProfilViewModel
        {
            UserId = user.Id,
            UserName = "prodavac1",
            Email = "prodavac1@example.com",
            Ime = "Demo",
            Prezime = "Prodavac",
            Password = "",
            ConfirmPassword = "",
            OdabraneRole = ["Prodavac"]
        });

        Assert.IsType<OkObjectResult>(editResult);

        user = await app.UserManager.FindByIdAsync(user.Id);
        Assert.Equal("prodavac1", user.UserName);
        Assert.Equal("prodavac1@example.com", user.Email);
        Assert.True(await app.UserManager.CheckPasswordAsync(user, "Valid123"));
        Assert.Contains("Prodavac", await app.UserManager.GetRolesAsync(user));

        var passwordResult = await admin.SaveProfil(new AddProfilViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Ime = user.Ime,
            Prezime = user.Prezime,
            Password = "Changed123",
            ConfirmPassword = "Changed123",
            OdabraneRole = ["Kupac"]
        });

        Assert.IsType<OkObjectResult>(passwordResult);

        user = await app.UserManager.FindByIdAsync(user.Id);
        Assert.True(await app.UserManager.CheckPasswordAsync(user, "Changed123"));

        var vehicle = await AddVehicleAsync(app.Db, "Audi", "A4", 43900);
        await AddRelatedUserDataAsync(app.Db, user, vehicle);

        var deleteResult = await admin.DeleteProfil(user.Id);

        Assert.IsType<OkObjectResult>(deleteResult);
        Assert.Null(await app.UserManager.FindByIdAsync(user.Id));
        Assert.False(await app.Db.Korpe.AnyAsync(k => k.KorisnikId == user.Id));
        Assert.False(await app.Db.StavkeKorpe.AnyAsync());
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id));
        Assert.False(await app.Db.PodrskaUpiti.AnyAsync(p => p.KorisnikId == user.Id));
        Assert.False(await app.Db.Narudzbe.AnyAsync(n => n.KorisnikId == user.Id));
        Assert.False(await app.Db.Placanja.AnyAsync());
    }

    [Fact]
    public async Task Admin_delete_review_removes_review_without_removing_user_or_vehicle()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "reviewer", "reviewer@example.com", "Kupac");
        var vehicle = await AddVehicleAsync(app.Db, "BMW", "X5", 89500);
        var review = new Recenzija
        {
            KorisnikId = user.Id,
            VoziloID = vehicle.VoziloID,
            Ocjena = 5,
            Komentar = "Odlicno vozilo.",
            DatumRecenzije = DateTime.UtcNow
        };
        app.Db.Recenzije.Add(review);
        await app.Db.SaveChangesAsync();

        var result = await CreateAdminController(app).DeleteRecenzija(review.RecenzijaID);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.RecenzijaID == review.RecenzijaID));
        Assert.NotNull(await app.UserManager.FindByIdAsync(user.Id));
        Assert.True(await app.Db.Vozila.AnyAsync(v => v.VoziloID == vehicle.VoziloID));
    }

    [Fact]
    public async Task Admin_delete_vehicle_removes_reviews_and_cart_items_and_updates_cart_total()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "cartuser", "cartuser@example.com", "Kupac");
        var vehicle = await AddVehicleAsync(app.Db, "Tesla", "Model 3", 72800);

        var cart = new Korpa { KorisnikId = user.Id, UkupnaCijena = 72800 };
        app.Db.Korpe.Add(cart);
        await app.Db.SaveChangesAsync();

        app.Db.StavkeKorpe.Add(new StavkaKorpe
        {
            KorpaID = cart.KorpaID,
            VoziloID = vehicle.VoziloID,
            Kolicina = 1,
            CijenaStavke = 72800
        });
        app.Db.Recenzije.Add(new Recenzija
        {
            KorisnikId = user.Id,
            VoziloID = vehicle.VoziloID,
            Ocjena = 4,
            Komentar = "Dobra kupovina.",
            DatumRecenzije = DateTime.UtcNow
        });
        await app.Db.SaveChangesAsync();

        var result = await CreateAdminController(app).DeleteVozilo(vehicle.VoziloID);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.VoziloID == vehicle.VoziloID));
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.VoziloID == vehicle.VoziloID));
        Assert.False(await app.Db.StavkeKorpe.AnyAsync(s => s.VoziloID == vehicle.VoziloID));
        Assert.Equal(0, (await app.Db.Korpe.SingleAsync(k => k.KorpaID == cart.KorpaID)).UkupnaCijena);
    }

    [Fact]
    public async Task Admin_vehicle_crud_can_create_read_update_and_delete_vehicle()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        var createModel = CreateVehicleForm("Audi", "A8", 2024, 119000, "Dizel");
        createModel.Slika = CreateImageFile("audi-a8.png", "image/png");

        var createResult = await admin.SaveVozilo(createModel);

        var createdOk = Assert.IsType<OkObjectResult>(createResult);
        var createdJson = ToJson(createdOk);
        var vehicleId = createdJson.GetProperty("voziloId").GetInt32();
        var vehicle = await app.Db.Vozila.SingleAsync(v => v.VoziloID == vehicleId);
        Assert.Equal("Audi", vehicle.Marka);
        Assert.Equal("A8", vehicle.Model);
        Assert.False(string.IsNullOrWhiteSpace(vehicle.Slika));

        var listJson = ToJson(await admin.GetVozilaJson(searchQuery: "Audi"));
        Assert.Contains(listJson.GetProperty("vozila").EnumerateArray(), item => item.GetProperty("voziloID").GetInt32() == vehicleId);

        var updateModel = CreateVehicleForm("Audi", "A8 L", 2025, 129000, "Hibrid");
        updateModel.VoziloID = vehicleId;

        var updateResult = await admin.SaveVozilo(updateModel);

        Assert.IsType<OkObjectResult>(updateResult);
        vehicle = await app.Db.Vozila.SingleAsync(v => v.VoziloID == vehicleId);
        Assert.Equal("A8 L", vehicle.Model);
        Assert.Equal(TipGoriva.Hibrid, vehicle.Gorivo);
        Assert.Equal(129000, vehicle.Cijena);

        var deleteResult = await admin.DeleteVozilo(vehicleId);

        Assert.IsType<OkObjectResult>(deleteResult);
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.VoziloID == vehicleId));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_missing_image_for_new_vehicle_and_missing_vehicle_for_update()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        var missingImageResult = await admin.SaveVozilo(CreateVehicleForm("BMW", "M5", 2024, 145000, "Benzin"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(missingImageResult);
        Assert.Contains("Slika", JsonSerializer.Serialize(badRequest.Value));

        var updateMissing = CreateVehicleForm("BMW", "M5", 2024, 145000, "Benzin");
        updateMissing.VoziloID = 9999;

        var missingUpdateResult = await admin.SaveVozilo(updateMissing);

        Assert.IsType<NotFoundResult>(missingUpdateResult);
    }

    [Fact]
    public async Task Customer_flow_can_register_login_add_to_cart_buy_review_and_admin_delete_cleans_everything()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "flowbuyer", "flowbuyer@example.com", "Kupac");
        Assert.True(await app.UserManager.CheckPasswordAsync(user, "Valid123"));

        var vehicle = await AddVehicleAsync(app.Db, "Mercedes", "GLC", 56900);

        var cartController = CreateKorpaController(app, user);
        await cartController.DodajUKorpu(vehicle.VoziloID);

        var cart = await app.Db.Korpe.Include(k => k.StavkeKorpe).SingleAsync(k => k.KorisnikId == user.Id);
        Assert.Single(cart.StavkeKorpe);
        Assert.Equal(56900, cart.UkupnaCijena);

        await cartController.IzvrsiPlacanje(
            vehicle.VoziloID,
            "Demo Kupac",
            "4242424242424242",
            "12/30",
            "123");

        Assert.True(await app.Db.Narudzbe.AnyAsync(n => n.KorisnikId == user.Id));
        Assert.True(await app.Db.Placanja.AnyAsync());
        Assert.False(await app.Db.StavkeKorpe.AnyAsync(s => s.KorpaID == cart.KorpaID));
        Assert.Equal(0, (await app.Db.Korpe.SingleAsync(k => k.KorpaID == cart.KorpaID)).UkupnaCijena);

        await CreateProfilController(app, user).DodajRecenziju(vehicle.VoziloID, 5, "Sve preporuke.");

        Assert.True(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id && r.VoziloID == vehicle.VoziloID));

        var deleteResult = await CreateAdminController(app).DeleteProfil(user.Id);

        Assert.IsType<OkObjectResult>(deleteResult);
        Assert.Null(await app.UserManager.FindByIdAsync(user.Id));
        Assert.False(await app.Db.Korpe.AnyAsync(k => k.KorisnikId == user.Id));
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id));
        Assert.False(await app.Db.Narudzbe.AnyAsync(n => n.KorisnikId == user.Id));
        Assert.False(await app.Db.Placanja.AnyAsync());
    }

    [Fact]
    public async Task Account_register_creates_buyer_with_valid_username_and_rejects_duplicates()
    {
        await using var app = await TestApp.CreateAsync();
        var account = CreateAccountController(app);

        var result = await account.Register(new RegisterViewModel
        {
            Ime = "Novi",
            Prezime = "Kupac",
            UserName = "novikupac",
            Email = "novikupac@example.com",
            Password = "Valid123",
            ConfirmPassword = "Valid123"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var user = await app.UserManager.FindByNameAsync("novikupac");
        Assert.NotNull(user);
        Assert.Equal("novikupac@example.com", user.Email);
        Assert.Contains("Kupac", await app.UserManager.GetRolesAsync(user));

        var duplicateResult = await account.Register(new RegisterViewModel
        {
            Ime = "Novi",
            Prezime = "Drugi",
            UserName = "novikupac",
            Email = "drugi@example.com",
            Password = "Valid123",
            ConfirmPassword = "Valid123"
        });

        Assert.IsType<ViewResult>(duplicateResult);
        Assert.False(account.ModelState.IsValid);
    }

    [Fact]
    public async Task Admin_profile_json_filters_by_role_and_search_without_leaking_layout_fields()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        await CreateUserAsync(app, "adminpaneluser", "adminpaneluser@example.com", "Administrator");
        await CreateUserAsync(app, "sellerpaneluser", "sellerpaneluser@example.com", "Prodavac");
        await CreateUserAsync(app, "buyerpaneluser", "buyerpaneluser@example.com", "Kupac");

        var sellerJson = ToJson(await admin.GetProfiliJson(searchQuery: "seller", roleFilter: "Prodavac"));
        var sellerProfiles = sellerJson.GetProperty("profili").EnumerateArray().ToList();

        Assert.Single(sellerProfiles);
        Assert.Equal("sellerpaneluser", sellerProfiles[0].GetProperty("userName").GetString());
        Assert.Equal("sellerpaneluser@example.com", sellerProfiles[0].GetProperty("email").GetString());
        Assert.Equal("Prodavac", sellerProfiles[0].GetProperty("uloga").GetString());

        var buyerJson = ToJson(await admin.GetProfiliJson(roleFilter: "Kupac"));
        var buyerNames = buyerJson.GetProperty("profili")
            .EnumerateArray()
            .Select(user => user.GetProperty("userName").GetString())
            .ToList();

        Assert.Contains("buyerpaneluser", buyerNames);
        Assert.DoesNotContain("sellerpaneluser", buyerNames);
        Assert.DoesNotContain("adminpaneluser", buyerNames);
    }

    [Fact]
    public async Task Admin_support_status_can_change_and_invalid_status_is_rejected()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "supportbuyer", "supportbuyer@example.com", "Kupac");
        var ticket = new Podrska
        {
            KorisnikId = user.Id,
            Naslov = "Status test",
            Sadrzaj = "Molim promjenu statusa.",
            DatumUpita = DateTime.UtcNow.AddMinutes(-5),
            Status = StatusUpita.Poslat
        };
        app.Db.PodrskaUpiti.Add(ticket);
        await app.Db.SaveChangesAsync();

        var admin = CreateAdminController(app);
        var validResult = await admin.UpdatePodrskaStatus(ticket.UpitID, "UObradi");

        Assert.IsType<OkObjectResult>(validResult);
        Assert.Equal(StatusUpita.UObradi, (await app.Db.PodrskaUpiti.FindAsync(ticket.UpitID)).Status);

        var invalidResult = await admin.UpdatePodrskaStatus(ticket.UpitID, "NijeStatus");

        Assert.IsType<BadRequestObjectResult>(invalidResult);
        Assert.Equal(StatusUpita.UObradi, (await app.Db.PodrskaUpiti.FindAsync(ticket.UpitID)).Status);
    }

    [Fact]
    public async Task Admin_support_json_orders_filters_and_never_requires_message_preview_in_table_contract()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "supportjson", "supportjson@example.com", "Kupac");
        app.Db.PodrskaUpiti.AddRange(
            new Podrska
            {
                KorisnikId = user.Id,
                Naslov = "Prvi upit",
                Sadrzaj = "Dugi sadrzaj koji treba ostati samo za modal prikaz.",
                DatumUpita = DateTime.UtcNow.AddDays(-2),
                Status = StatusUpita.Poslat
            },
            new Podrska
            {
                KorisnikId = user.Id,
                Naslov = "Zadnji upit",
                Sadrzaj = "Tekst za pretragu statusa.",
                DatumUpita = DateTime.UtcNow,
                Status = StatusUpita.Odgovoren
            });
        await app.Db.SaveChangesAsync();

        var json = ToJson(await CreateAdminController(app).GetPodrskaJson(searchQuery: "Zadnji"));
        var tickets = json.GetProperty("upiti").EnumerateArray().ToList();

        Assert.Single(tickets);
        Assert.Equal("Zadnji upit", tickets[0].GetProperty("naslov").GetString());
        Assert.Equal("supportjson@example.com", tickets[0].GetProperty("korisnikEmail").GetString());
        Assert.Equal("Odgovoren", tickets[0].GetProperty("status").GetString());
        Assert.Equal("Tekst za pretragu statusa.", tickets[0].GetProperty("sadrzaj").GetString());
    }

    [Fact]
    public async Task Admin_review_json_filters_by_user_vehicle_and_comment()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "reviewfilter", "reviewfilter@example.com", "Kupac");
        var audi = await AddVehicleAsync(app.Db, "Audi", "A6", 62000);
        var bmw = await AddVehicleAsync(app.Db, "BMW", "M3", 99000);

        app.Db.Recenzije.AddRange(
            new Recenzija
            {
                KorisnikId = user.Id,
                VoziloID = audi.VoziloID,
                Ocjena = 5,
                Komentar = "Komentar za trazenje.",
                DatumRecenzije = DateTime.UtcNow
            },
            new Recenzija
            {
                KorisnikId = user.Id,
                VoziloID = bmw.VoziloID,
                Ocjena = 2,
                Komentar = "Drugi komentar.",
                DatumRecenzije = DateTime.UtcNow.AddDays(-1)
            });
        await app.Db.SaveChangesAsync();

        var json = ToJson(await CreateAdminController(app).GetRecenzijeJson(
            searchQuery: "trazenje",
            korisnikFilter: "reviewfilter",
            voziloFilter: "Audi"));
        var reviews = json.GetProperty("recenzije").EnumerateArray().ToList();

        Assert.Single(reviews);
        Assert.Equal("reviewfilter", reviews[0].GetProperty("korisnikUserName").GetString());
        Assert.Equal("Audi A6", reviews[0].GetProperty("voziloNaziv").GetString());
        Assert.Equal(5, reviews[0].GetProperty("ocjena").GetInt32());
    }

    [Theory]
    [InlineData("cijena", "desc", "Expensive", "Mid", "Cheap")]
    [InlineData("cijena", "asc", "Cheap", "Mid", "Expensive")]
    [InlineData("godiste", "desc", "Mid", "Expensive", "Cheap")]
    [InlineData("godiste", "asc", "Cheap", "Expensive", "Mid")]
    [InlineData("kilometraza", "asc", "Cheap", "Mid", "Expensive")]
    [InlineData("kilometraza", "desc", "Expensive", "Mid", "Cheap")]
    public async Task Admin_vehicle_json_sorting_uses_expected_single_active_sort(string sort, string direction, params string[] expectedModels)
    {
        await using var app = await TestApp.CreateAsync();
        await AddVehicleWithDetailsAsync(app.Db, "Sort", "Cheap", 2018, 10000, 12000, TipGoriva.Benzin);
        await AddVehicleWithDetailsAsync(app.Db, "Sort", "Mid", 2024, 50000, 42000, TipGoriva.Hibrid);
        await AddVehicleWithDetailsAsync(app.Db, "Sort", "Expensive", 2020, 90000, 99000, TipGoriva.Elektro);

        var json = ToJson(await CreateAdminController(app).GetVozilaJson(sort: sort, direction: direction));
        var models = json.GetProperty("vozila")
            .EnumerateArray()
            .Select(item => item.GetProperty("naziv").GetString()!.Replace("Sort ", ""))
            .ToList();

        Assert.Equal(expectedModels, models);
        Assert.Equal(sort, json.GetProperty("sort").GetString());
        Assert.Equal(direction, json.GetProperty("direction").GetString());
    }

    [Fact]
    public async Task Admin_vehicle_json_filters_by_fuel_and_search_without_resetting_sort()
    {
        await using var app = await TestApp.CreateAsync();
        await AddVehicleWithDetailsAsync(app.Db, "Audi", "Dizel", 2018, 40000, 39000, TipGoriva.Dizel);
        await AddVehicleWithDetailsAsync(app.Db, "Audi", "Elektro", 2023, 20000, 89000, TipGoriva.Elektro);
        await AddVehicleWithDetailsAsync(app.Db, "BMW", "Dizel", 2022, 90000, 54000, TipGoriva.Dizel);

        var json = ToJson(await CreateAdminController(app).GetVozilaJson(
            searchQuery: "Audi",
            sort: "cijena",
            direction: "desc",
            gorivoFilter: "Dizel"));
        var vehicles = json.GetProperty("vozila").EnumerateArray().ToList();

        Assert.Single(vehicles);
        Assert.Equal("Audi Dizel", vehicles[0].GetProperty("naziv").GetString());
        Assert.Equal("Dizel", vehicles[0].GetProperty("gorivo").GetString());
        Assert.Equal("cijena", json.GetProperty("sort").GetString());
        Assert.Equal("desc", json.GetProperty("direction").GetString());
    }

    private static AdminPanelController CreateAdminController(TestApp app)
    {
        var controller = new AdminPanelController(
            app.Db,
            app.Environment,
            app.UserManager,
            app.RoleManager);

        ConfigureController(controller, app.Provider);
        return controller;
    }

    private static KorpaController CreateKorpaController(TestApp app, ApplicationUser user)
    {
        var controller = new KorpaController(
            app.Db,
            app.UserManager,
            NullLogger<KorpaController>.Instance,
            new MockPaymentService(NullLogger<MockPaymentService>.Instance),
            Options.Create(new StripeSettings { UseMockPayments = true }));

        ConfigureController(controller, app.Provider, user);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static AccountController CreateAccountController(TestApp app)
    {
        var controller = new AccountController(
            app.UserManager,
            app.SignInManager,
            app.RoleManager,
            NullLogger<AccountController>.Instance);

        ConfigureController(controller, app.Provider);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static ProfilController CreateProfilController(TestApp app, ApplicationUser user)
    {
        var controller = new ProfilController(
            app.UserManager,
            app.SignInManager,
            NullLogger<ProfilController>.Instance,
            app.Db);

        ConfigureController(controller, app.Provider, user);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static void ConfigureController(Controller controller, IServiceProvider provider, ApplicationUser user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        if (user != null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new Claim(ClaimTypes.Role, "Kupac")
            ], "TestAuth"));
            httpContext.Request.Headers.Referer = "/Vozilo";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
    }

    private static async Task<ApplicationUser> CreateUserAsync(TestApp app, string userName, string email, string role)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            Ime = "Demo",
            Prezime = "Korisnik"
        };

        var result = await app.UserManager.CreateAsync(user, "Valid123");
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));

        await app.UserManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task<Vozilo> AddVehicleAsync(ApplicationDbContext db, string brand, string model, decimal price)
    {
        var vehicle = await AddVehicleWithDetailsAsync(db, brand, model, 2022, 25000, price, TipGoriva.Benzin);
        return vehicle;
    }

    private static async Task<Vozilo> AddVehicleWithDetailsAsync(
        ApplicationDbContext db,
        string brand,
        string model,
        int year,
        int mileage,
        decimal price,
        TipGoriva fuel)
    {
        var vehicle = new Vozilo
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = "Crna",
            Kilometraza = mileage,
            Cijena = price,
            Opis = "Test vozilo"
        };
        db.Vozila.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    private static AddVoziloViewModel CreateVehicleForm(string brand, string model, int year, decimal price, string fuel)
    {
        return new AddVoziloViewModel
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = "Crna",
            Kilometraza = 18000,
            Cijena = price,
            Opis = "Detaljan opis test vozila."
        };
    }

    private static IFormFile CreateImageFile(string fileName, string contentType)
    {
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "Slika", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static JsonElement ToJson(JsonResult result)
    {
        return JsonSerializer.SerializeToElement(result.Value);
    }

    private static JsonElement ToJson(OkObjectResult result)
    {
        return JsonSerializer.SerializeToElement(result.Value);
    }

    private static async Task AddRelatedUserDataAsync(ApplicationDbContext db, ApplicationUser user, Vozilo vehicle)
    {
        var cart = new Korpa { KorisnikId = user.Id, UkupnaCijena = vehicle.Cijena ?? 0 };
        db.Korpe.Add(cart);
        await db.SaveChangesAsync();

        db.StavkeKorpe.Add(new StavkaKorpe
        {
            KorpaID = cart.KorpaID,
            VoziloID = vehicle.VoziloID,
            Kolicina = 1,
            CijenaStavke = vehicle.Cijena ?? 0
        });

        db.Recenzije.Add(new Recenzija
        {
            KorisnikId = user.Id,
            VoziloID = vehicle.VoziloID,
            Ocjena = 5,
            Komentar = "Odlicno.",
            DatumRecenzije = DateTime.UtcNow
        });

        db.PodrskaUpiti.Add(new Podrska
        {
            KorisnikId = user.Id,
            Naslov = "Pomoc",
            Sadrzaj = "Test upit",
            DatumUpita = DateTime.UtcNow,
            Status = StatusUpita.Poslat
        });

        var order = new Narudzba
        {
            KorisnikId = user.Id,
            DatumNarudzbe = DateTime.UtcNow,
            Status = StatusNarudzbe.Placena,
            UkupnaCijena = vehicle.Cijena ?? 0
        };
        db.Narudzbe.Add(order);
        await db.SaveChangesAsync();

        db.StavkeKorpe.Add(new StavkaKorpe
        {
            NarudzbaID = order.NarudzbaID,
            VoziloID = vehicle.VoziloID,
            Kolicina = 1,
            CijenaStavke = vehicle.Cijena ?? 0
        });
        db.Placanja.Add(new Placanje
        {
            NarudzbaID = order.NarudzbaID,
            DatumPlacanja = DateTime.UtcNow,
            Iznos = vehicle.Cijena ?? 0,
            Status = StatusPlacanja.Uspjesno
        });

        await db.SaveChangesAsync();
    }

    private sealed class TestApp : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _rootProvider;
        private readonly IServiceScope _scope;

        private TestApp(SqliteConnection connection, ServiceProvider rootProvider, IServiceScope scope)
        {
            _connection = connection;
            _rootProvider = rootProvider;
            _scope = scope;
            Provider = scope.ServiceProvider;
            Db = Provider.GetRequiredService<ApplicationDbContext>();
            UserManager = Provider.GetRequiredService<UserManager<ApplicationUser>>();
            RoleManager = Provider.GetRequiredService<RoleManager<IdentityRole>>();
            SignInManager = Provider.GetRequiredService<SignInManager<ApplicationUser>>();
            Environment = new TestWebHostEnvironment();
        }

        public IServiceProvider Provider { get; }
        public ApplicationDbContext Db { get; }
        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }
        public SignInManager<ApplicationUser> SignInManager { get; }
        public IWebHostEnvironment Environment { get; }

        public static async Task<TestApp> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            var rootProvider = services.BuildServiceProvider();
            var scope = rootProvider.CreateScope();
            var app = new TestApp(connection, rootProvider, scope);

            await app.Db.Database.EnsureCreatedAsync();
            foreach (var role in new[] { "Administrator", "Prodavac", "Kupac" })
            {
                if (!await app.RoleManager.RoleExistsAsync(role))
                {
                    await app.RoleManager.CreateAsync(new IdentityRole(role));
                }
            }

            return app;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            _scope.Dispose();
            await _connection.DisposeAsync();
            await _rootProvider.DisposeAsync();
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment()
        {
            Directory.CreateDirectory(WebRootPath);
        }

        public string ApplicationName { get; set; } = "AutosalonOneZone.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), $"autosalon-webroot-{Guid.NewGuid():N}");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class TestUrlHelper : IUrlHelper
    {
        public TestUrlHelper(ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; }

        public string Action(UrlActionContext actionContext)
        {
            var controller = string.IsNullOrWhiteSpace(actionContext.Controller)
                ? "Korpa"
                : actionContext.Controller;

            return $"/{controller}/{actionContext.Action}";
        }

        public string Content(string contentPath) => contentPath;
        public bool IsLocalUrl(string url) => true;
        public string Link(string routeName, object values) => routeName;
        public string RouteUrl(UrlRouteContext routeContext) => routeContext.RouteName ?? "/";
    }
}
