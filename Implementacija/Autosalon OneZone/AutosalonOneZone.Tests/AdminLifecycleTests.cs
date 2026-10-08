#nullable disable

using System.Security.Claims;
using System.Text;
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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

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
        Assert.Equal(TipBoje.Crna, vehicle.Boja);
        Assert.False(string.IsNullOrWhiteSpace(vehicle.Slika));
        Assert.EndsWith(".webp", vehicle.Slika, StringComparison.Ordinal);
        var uploadFolder = Path.Combine(app.Environment.WebRootPath, "vehicle-uploads");
        Assert.True(File.Exists(Path.Combine(uploadFolder, vehicle.Slika)));
        Assert.True(File.Exists(Path.Combine(uploadFolder, VehicleImageNames.ThumbnailFor(vehicle.Slika))));

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
    public async Task Admin_vehicle_save_rejects_uploaded_file_with_invalid_image_signature()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("BMW", "M5", 2024, 145000, "Benzin");
        model.Slika = CreateFile("fake.png", "image/png", [0x41, 0x42, 0x43, 0x44]);

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("validna slika", JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "BMW" && v.Model == "M5"));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_corrupt_image_with_valid_signature()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("BMW", "M6", 2024, 155000, "Benzin");
        model.Slika = CreateFile(
            "corrupt.png",
            "image/png",
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ImageProcessingError", JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "BMW" && v.Model == "M6"));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_optimized_image_larger_than_two_megabytes()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("BMW", "M7", 2024, 165000, "Benzin");
        var content = new byte[(2 * 1024 * 1024) + 1];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(content, 0);
        model.Slika = CreateFile("oversized.png", "image/png", content);

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ImageSizeLimitError", JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "BMW" && v.Model == "M7"));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_invalid_image_content_type_without_inserting_vehicle()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("Porsche", "Panamera", 2024, 155000, "Benzin");
        model.Slika = CreateImageFile("panamera.png", "application/octet-stream");

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("validna slika", JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "Porsche" && v.Model == "Panamera"));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_invalid_fuel_before_file_is_persisted()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("Rimac", "Nevera", 2024, 2400000, "Steam");
        model.Slika = CreateImageFile("nevera.png", "image/png");
        var uploadFolder = Path.Combine(app.Environment.WebRootPath, "images/vozila");

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Gorivo", JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "Rimac" && v.Model == "Nevera"));
        Assert.False(Directory.Exists(uploadFolder));
    }

    [Fact]
    public async Task Admin_vehicle_save_rejects_undefined_color()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);
        var model = CreateVehicleForm("Volvo", "XC90", 2024, 98000, "Hibrid");
        model.Boja = (TipBoje)999;
        model.Slika = CreateImageFile("volvo-xc90.png", "image/png");

        var result = await admin.SaveVozilo(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(nameof(model.Boja), JsonSerializer.Serialize(badRequest.Value));
        Assert.False(await app.Db.Vozila.AnyAsync(v => v.Marka == "Volvo" && v.Model == "XC90"));
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

        await cartController.IzvrsiPlacanjeSvih(
            JsonSerializer.Serialize(new[] { new { id = vehicle.VoziloID, naziv = "Mercedes GLC", cijena = 56900m } }),
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
    public async Task Buy_now_payment_validates_missing_fields_and_cleans_existing_cart_item_on_success()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "singlebuyer", "singlebuyer@example.com", "Kupac");
        var vehicle = await AddVehicleAsync(app.Db, "Audi", "Q8", 112000);
        var controller = CreateKorpaController(app, user);

        var invalidResult = await controller.IzvrsiPlacanje(vehicle.VoziloID, "", null!, null!, null!);

        var invalidJson = ToJson(Assert.IsType<JsonResult>(invalidResult));
        Assert.False(invalidJson.GetProperty("success").GetBoolean());
        var errors = invalidJson.GetProperty("errors");
        Assert.True(errors.TryGetProperty("imeVlasnika", out _));
        Assert.True(errors.TryGetProperty("brojKartice", out _));
        Assert.True(errors.TryGetProperty("datumIsteka", out _));
        Assert.True(errors.TryGetProperty("cvv", out _));
        Assert.False(await app.Db.Narudzbe.AnyAsync(n => n.KorisnikId == user.Id));

        await controller.DodajUKorpu(vehicle.VoziloID);
        var cart = await app.Db.Korpe.Include(k => k.StavkeKorpe).SingleAsync(k => k.KorisnikId == user.Id);
        Assert.Single(cart.StavkeKorpe);

        var validResult = await controller.IzvrsiPlacanje(vehicle.VoziloID, "Demo Kupac", "4242424242424242", "12/30", "123");

        var validJson = ToJson(Assert.IsType<JsonResult>(validResult));
        Assert.True(validJson.GetProperty("success").GetBoolean());
        Assert.Contains("/Korpa/Uspjeh", validJson.GetProperty("redirectUrl").GetString());
        Assert.True(await app.Db.Narudzbe.AnyAsync(n => n.KorisnikId == user.Id));
        Assert.True(await app.Db.Placanja.AnyAsync());
        Assert.True(await app.Db.StavkeKorpe.AnyAsync(s => s.VoziloID == vehicle.VoziloID && s.NarudzbaID != null));
        Assert.False(await app.Db.StavkeKorpe.AnyAsync(s => s.KorpaID == cart.KorpaID && s.VoziloID == vehicle.VoziloID));
        Assert.Equal(0, (await app.Db.Korpe.SingleAsync(k => k.KorpaID == cart.KorpaID)).UkupnaCijena);
    }

    [Fact]
    public async Task Purchased_vehicle_review_rejects_empty_and_overlong_comments_without_creating_review()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "reviewbuyer", "reviewbuyer@example.com", "Kupac");
        var vehicle = await AddVehicleAsync(app.Db, "Volkswagen", "Golf", 28900);
        await AddPurchasedVehicleAsync(app.Db, user, vehicle);
        var controller = CreateProfilController(app, user);

        await controller.DodajRecenziju(vehicle.VoziloID, 5, "   ");
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id && r.VoziloID == vehicle.VoziloID));

        await controller.DodajRecenziju(vehicle.VoziloID, 5, new string('a', 1001));
        Assert.False(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id && r.VoziloID == vehicle.VoziloID));

        await controller.DodajRecenziju(vehicle.VoziloID, 5, "Korektan automobil.");
        Assert.True(await app.Db.Recenzije.AnyAsync(r => r.KorisnikId == user.Id && r.VoziloID == vehicle.VoziloID));
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
    public async Task Forgot_password_sends_reset_link_without_disclosing_unknown_emails()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "resetuser", "resetuser@example.com", "Kupac");
        var account = CreateAccountController(app);

        var knownEmailResult = await account.ForgotPassword(new ForgotPasswordViewModel
        {
            Email = user.Email!
        });

        var knownRedirect = Assert.IsType<RedirectToActionResult>(knownEmailResult);
        Assert.Equal("ForgotPasswordConfirmation", knownRedirect.ActionName);
        Assert.Single(app.EmailSender.Messages);
        Assert.Equal("resetuser@example.com", app.EmailSender.Messages[0].ToEmail);
        Assert.Contains("/Account/ResetPassword", app.EmailSender.Messages[0].ResetLink);

        var unknownEmailResult = await account.ForgotPassword(new ForgotPasswordViewModel
        {
            Email = "missing@example.com"
        });

        var unknownRedirect = Assert.IsType<RedirectToActionResult>(unknownEmailResult);
        Assert.Equal("ForgotPasswordConfirmation", unknownRedirect.ActionName);
        Assert.Single(app.EmailSender.Messages);
    }

    [Fact]
    public async Task Reset_password_accepts_valid_identity_token_and_rejects_invalid_code()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "resetflow", "resetflow@example.com", "Kupac");
        var account = CreateAccountController(app);
        var token = await app.UserManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetResult = await account.ResetPassword(new ResetPasswordViewModel
        {
            UserId = user.Id,
            Code = encodedToken,
            Password = "NewValid123",
            ConfirmPassword = "NewValid123"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resetResult);
        Assert.Equal("ResetPasswordConfirmation", redirect.ActionName);
        Assert.True(await app.UserManager.CheckPasswordAsync(user, "NewValid123"));

        var invalidAccount = CreateAccountController(app);
        var invalidResult = await invalidAccount.ResetPassword(new ResetPasswordViewModel
        {
            UserId = user.Id,
            Code = "not-a-valid-token",
            Password = "OtherValid123",
            ConfirmPassword = "OtherValid123"
        });

        Assert.IsType<ViewResult>(invalidResult);
        Assert.False(invalidAccount.ModelState.IsValid);
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
    public async Task Admin_support_ticket_can_be_taken_and_replied_to()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "supportbuyer", "supportbuyer@example.com", "Kupac");
        var adminUser = await CreateUserAsync(app, "supportadmin", "supportadmin@example.com", "Administrator");
        var ticket = await AddSupportTicketAsync(app.Db, user, "Status test", "Molim promjenu statusa.");
        var admin = CreateAdminController(app, adminUser, "Administrator");

        Assert.IsType<OkObjectResult>(await admin.PreuzmiPodrsku(ticket.UpitID, ""));
        Assert.IsType<OkObjectResult>(await admin.OdgovoriNaPodrsku(ticket.UpitID, new SupportStaffReplyViewModel
        {
            Message = "Poštovani, provjerili smo Vaš upit.",
            RowVersion = ""
        }));

        var updated = await app.Db.PodrskaUpiti
            .Include(item => item.Poruke)
            .SingleAsync(item => item.UpitID == ticket.UpitID);
        Assert.Equal(StatusUpita.CekaKorisnika, updated.Status);
        Assert.Equal(adminUser.Id, updated.DodijeljenKorisnikId);
        Assert.Contains(updated.Poruke, message =>
            message.TipAutora == TipAutoraPorukePodrske.Osoblje &&
            message.Sadrzaj == "Poštovani, provjerili smo Vaš upit.");
    }

    [Fact]
    public async Task Support_assignment_response_contains_current_agent_and_release_clears_it()
    {
        await using var app = await TestApp.CreateAsync();
        var customer = await CreateUserAsync(app, "assignmentbuyer", "assignmentbuyer@example.com", "Kupac");
        var agent = await CreateUserAsync(app, "assignmentagent", "assignmentagent@example.com", "Prodavac");
        var support = CreateSupportService(app);
        var ticketId = await support.CreateTicketAsync(
            customer.Id,
            "Provjera dodjele",
            "Potrebna mi je pomoć sa informacijama o vozilu.",
            "bs-Latn-BA");

        var taken = await support.TakeAsync(ticketId, agent.Id, "", false);

        Assert.Equal(SupportOperationStatus.Success, taken.Status);
        Assert.Equal(agent.Id, taken.Conversation?.AssignedAgentId);
        Assert.Equal("Demo Korisnik", taken.Conversation?.AssignedAgentName);

        var released = await support.ReleaseAsync(ticketId, agent.Id, "", false);

        Assert.Equal(SupportOperationStatus.Success, released.Status);
        Assert.Null(released.Conversation?.AssignedAgentId);
        Assert.Equal(string.Empty, released.Conversation?.AssignedAgentName);
    }

    [Fact]
    public async Task Seller_cannot_reply_to_a_ticket_assigned_to_another_agent()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "assignedbuyer", "assignedbuyer@example.com", "Kupac");
        var firstSeller = await CreateUserAsync(app, "sellerone", "sellerone@example.com", "Prodavac");
        var secondSeller = await CreateUserAsync(app, "sellertwo", "sellertwo@example.com", "Prodavac");
        var ticket = await AddSupportTicketAsync(app.Db, user, "Dodjela", "Potrebna mi je pomoć oko kupovine.");

        Assert.IsType<OkObjectResult>(await CreateAdminController(app, firstSeller, "Prodavac")
            .PreuzmiPodrsku(ticket.UpitID, ""));
        var result = await CreateAdminController(app, secondSeller, "Prodavac")
            .OdgovoriNaPodrsku(ticket.UpitID, new SupportStaffReplyViewModel
            {
                Message = "Ovaj odgovor ne smije biti sačuvan.",
                RowVersion = ""
            });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.DoesNotContain(await app.Db.PorukePodrske.ToListAsync(), message =>
            message.Sadrzaj == "Ovaj odgovor ne smije biti sačuvan.");
    }

    [Fact]
    public async Task Admin_support_take_missing_ticket_returns_not_found()
    {
        await using var app = await TestApp.CreateAsync();
        var adminUser = await CreateUserAsync(app, "missingadmin", "missingadmin@example.com", "Administrator");

        var result = await CreateAdminController(app, adminUser, "Administrator").PreuzmiPodrsku(404404, "");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Customer_follow_up_keeps_assigned_ticket_in_progress_and_closed_ticket_can_be_reopened()
    {
        await using var app = await TestApp.CreateAsync();
        var customer = await CreateUserAsync(app, "flowbuyer", "flowbuyer@example.com", "Kupac");
        var agent = await CreateUserAsync(app, "flowagent", "flowagent@example.com", "Prodavac");
        var support = CreateSupportService(app);
        var ticketId = await support.CreateTicketAsync(
            customer.Id,
            "Cijeli tok",
            "Trebaju mi dodatne informacije o finansiranju.",
            "bs-Latn-BA");

        Assert.Equal(SupportOperationStatus.Success, (await support.TakeAsync(ticketId, agent.Id, "", false)).Status);
        Assert.Equal(SupportOperationStatus.Success, (await support.ReplyAsync(
            ticketId,
            agent.Id,
            new SupportStaffReplyViewModel
            {
                Message = "Koji period otplate Vam najviše odgovara?",
                RowVersion = ""
            },
            "https://example.test/Profil/PodrskaDetalji/1")).Status);

        var followUp = await support.SendUserMessageAsync(
            ticketId,
            customer.Id,
            "Najviše bi mi odgovarao period od pet godina.");
        Assert.Equal(SupportOperationStatus.Success, followUp.Status);
        Assert.Equal(StatusUpita.UObradi, followUp.Conversation?.Status);
        Assert.Equal(agent.Id, followUp.Conversation?.AssignedAgentId);

        Assert.Equal(SupportOperationStatus.Success, (await support.CloseAsync(ticketId, agent.Id, "", false)).Status);
        var reopen = await support.ReopenAsync(ticketId, customer.Id);
        Assert.Equal(SupportOperationStatus.Success, reopen.Status);
        Assert.Equal(StatusUpita.UObradi, reopen.Conversation?.Status);
        Assert.Contains(reopen.Conversation!.Messages, message =>
            message.SenderType == TipAutoraPorukePodrske.Sistem &&
            message.Content == "SupportSystemReopened");
    }

    [Fact]
    public async Task Staff_reply_queues_email_only_outside_demo_mode()
    {
        await using var app = await TestApp.CreateAsync();
        var customer = await CreateUserAsync(app, "emailbuyer", "emailbuyer@example.com", "Kupac");
        var agent = await CreateUserAsync(app, "emailagent", "emailagent@example.com", "Prodavac");
        var support = CreateSupportService(app, demoEnabled: false, emailEnabled: true);
        var ticketId = await support.CreateTicketAsync(
            customer.Id,
            "Email odgovor",
            "Molim odgovor i putem elektronske pošte.",
            "en-US");

        await support.TakeAsync(ticketId, agent.Id, "", false);
        var result = await support.ReplyAsync(
            ticketId,
            agent.Id,
            new SupportStaffReplyViewModel
            {
                Message = "Your support response is ready in the application.",
                RowVersion = ""
            },
            "https://example.test/Profil/PodrskaDetalji/1");

        Assert.Equal(SupportOperationStatus.Success, result.Status);
        var outbox = await app.Db.EmailPodrskeOutbox.SingleAsync();
        Assert.Equal(customer.Email, outbox.Primalac);
        Assert.Equal("en-US", outbox.Jezik);
        Assert.Equal("https://example.test/Profil/PodrskaDetalji/1", outbox.DetaljiUrl);
        Assert.Null(outbox.PoslanoUtc);
    }

    [Fact]
    public async Task Admin_profile_save_rejects_invalid_password_without_creating_user()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        var result = await admin.SaveProfil(new AddProfilViewModel
        {
            UserName = "weakpassworduser",
            Email = "weakpassworduser@example.com",
            Ime = "Weak",
            Prezime = "Password",
            Password = "weak",
            ConfirmPassword = "weak",
            OdabraneRole = ["Kupac"]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Lozinka mora imati najmanje 8 karaktera", JsonSerializer.Serialize(badRequest.Value));
        Assert.Null(await app.UserManager.FindByNameAsync("weakpassworduser"));
    }

    [Fact]
    public async Task Admin_profile_role_change_replaces_existing_roles()
    {
        await using var app = await TestApp.CreateAsync();
        var user = await CreateUserAsync(app, "rolechange", "rolechange@example.com", "Kupac");
        await app.UserManager.AddToRoleAsync(user, "Prodavac");

        var result = await CreateAdminController(app).SaveProfil(new AddProfilViewModel
        {
            UserId = user.Id,
            UserName = "rolechange",
            Email = "rolechange@example.com",
            Ime = "Role",
            Prezime = "Change",
            Password = "",
            ConfirmPassword = "",
            OdabraneRole = ["Administrator"]
        });

        Assert.IsType<OkObjectResult>(result);

        var roles = await app.UserManager.GetRolesAsync(user);
        Assert.Equal(["Administrator"], roles.OrderBy(role => role).ToArray());
    }

    [Fact]
    public async Task Admin_profile_missing_edit_and_delete_return_not_found()
    {
        await using var app = await TestApp.CreateAsync();
        var admin = CreateAdminController(app);

        var editResult = await admin.GetEditProfilForm("missing-user-id");
        var saveResult = await admin.SaveProfil(new AddProfilViewModel
        {
            UserId = "missing-user-id",
            UserName = "missing",
            Email = "missing@example.com",
            Ime = "Missing",
            Prezime = "User",
            Password = "",
            ConfirmPassword = "",
            OdabraneRole = ["Kupac"]
        });
        var deleteResult = await admin.DeleteProfil("missing-user-id");

        Assert.IsType<NotFoundResult>(editResult);
        Assert.IsType<NotFoundResult>(saveResult);
        Assert.IsType<NotFoundResult>(deleteResult);
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
                DatumUpita = DateTime.UtcNow.AddDays(-2),
                DatumZadnjeAktivnosti = DateTime.UtcNow.AddDays(-2),
                Status = StatusUpita.CekaPodrsku,
                Poruke = [new PorukaPodrske { PosiljalacId = user.Id, TipAutora = TipAutoraPorukePodrske.Korisnik, Sadrzaj = "Dugi sadrzaj koji treba ostati samo za modal prikaz.", DatumSlanja = DateTime.UtcNow.AddDays(-2) }]
            },
            new Podrska
            {
                KorisnikId = user.Id,
                Naslov = "Zadnji upit",
                DatumUpita = DateTime.UtcNow,
                DatumZadnjeAktivnosti = DateTime.UtcNow,
                Status = StatusUpita.CekaKorisnika,
                Poruke = [new PorukaPodrske { PosiljalacId = user.Id, TipAutora = TipAutoraPorukePodrske.Korisnik, Sadrzaj = "Tekst za pretragu statusa.", DatumSlanja = DateTime.UtcNow }]
            });
        await app.Db.SaveChangesAsync();

        var json = ToJson(await CreateAdminController(app).GetPodrskaJson(searchQuery: "Zadnji"));
        var tickets = json.GetProperty("upiti").EnumerateArray().ToList();

        Assert.Single(tickets);
        Assert.Equal("Zadnji upit", tickets[0].GetProperty("naslov").GetString());
        Assert.Equal("supportjson@example.com", tickets[0].GetProperty("korisnikEmail").GetString());
        Assert.Equal("CekaKorisnika", tickets[0].GetProperty("status").GetString());
        Assert.False(tickets[0].TryGetProperty("sadrzaj", out _));
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

    private static AdminPanelController CreateAdminController(TestApp app, ApplicationUser user = null, params string[] roles)
    {
        var controller = new AdminPanelController(
            new AdminDashboardService(app.Db),
            new AdminListQueryService(app.Db),
            new AdminModerationService(app.Db),
            new AdminVehicleService(
                app.Db,
                new VehicleImageStorage(
                    app.Environment,
                    Options.Create(new VehicleImageStorageOptions
                    {
                        UploadsPath = Path.Combine(app.Environment.WebRootPath, "vehicle-uploads")
                    }))),
            new AdminProfileService(app.Db, app.UserManager, app.RoleManager),
            CreateSupportService(app));

        ConfigureController(controller, app.Provider, user, roles);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static KorpaController CreateKorpaController(TestApp app, ApplicationUser user)
    {
        var controller = new KorpaController(
            app.UserManager,
            NullLogger<KorpaController>.Instance,
            new CartService(app.Db),
            new CheckoutService(
                app.Db,
                new MockPaymentService(NullLogger<MockPaymentService>.Instance),
                NullLogger<CheckoutService>.Instance));

        ConfigureController(controller, app.Provider, user);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static AccountController CreateAccountController(TestApp app)
    {
        var controller = new AccountController(
            new AccountAuthenticationService(app.UserManager, app.SignInManager),
            new AccountRegistrationService(
                app.UserManager,
                app.RoleManager,
                NullLogger<AccountRegistrationService>.Instance),
            new PasswordRecoveryService(
                app.UserManager,
                app.EmailSender,
                NullLogger<PasswordRecoveryService>.Instance),
            NullLogger<AccountController>.Instance);

        ConfigureController(controller, app.Provider);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static ProfilController CreateProfilController(TestApp app, ApplicationUser user)
    {
        var controller = new ProfilController(
            app.UserManager,
            NullLogger<ProfilController>.Instance,
            new ProfileActivityService(app.Db),
            new ProfileAccountService(app.UserManager, app.SignInManager),
            CreateSupportService(app));

        ConfigureController(controller, app.Provider, user);
        controller.Url = new TestUrlHelper(controller.ControllerContext);
        return controller;
    }

    private static void ConfigureController(Controller controller, IServiceProvider provider, ApplicationUser user = null, params string[] roles)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        if (user != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
            };
            claims.AddRange((roles.Length == 0 ? ["Kupac"] : roles).Select(role => new Claim(ClaimTypes.Role, role)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            httpContext.Request.Headers.Referer = "/Vozilo";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
    }

    private static SupportService CreateSupportService(
        TestApp app,
        bool demoEnabled = true,
        bool emailEnabled = false) => new(
        app.Db,
        Options.Create(new DemoOptions { Enabled = demoEnabled }),
        Options.Create(new ResendEmailOptions { Enabled = emailEnabled }));

    private static async Task<Podrska> AddSupportTicketAsync(
        ApplicationDbContext db,
        ApplicationUser user,
        string title,
        string content)
    {
        var now = DateTime.UtcNow;
        var ticket = new Podrska
        {
            KorisnikId = user.Id,
            Naslov = title,
            DatumUpita = now,
            DatumZadnjeAktivnosti = now,
            Status = StatusUpita.CekaPodrsku,
            Poruke =
            [
                new PorukaPodrske
                {
                    PosiljalacId = user.Id,
                    TipAutora = TipAutoraPorukePodrske.Korisnik,
                    Sadrzaj = content,
                    DatumSlanja = now
                }
            ]
        };
        db.PodrskaUpiti.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
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

    private static async Task<Vozilo> AddVehicleAsync(ApplicationDbContext db, string brand, string model, int price)
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
        int price,
        TipGoriva fuel)
    {
        var vehicle = new Vozilo
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = TipBoje.Crna,
            Kilometraza = mileage,
            Cijena = price,
            Opis = "Test vozilo"
        };
        db.Vozila.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    private static AddVoziloViewModel CreateVehicleForm(string brand, string model, int year, int price, string fuel)
    {
        return new AddVoziloViewModel
        {
            Marka = brand,
            Model = model,
            Godiste = year,
            Gorivo = fuel,
            Kubikaza = 2.0m,
            Boja = TipBoje.Crna,
            Kilometraza = 18000,
            Cijena = price,
            Opis = "Detaljan opis test vozila."
        };
    }

    private static IFormFile CreateImageFile(string fileName, string contentType)
    {
        var content = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        return CreateFile(fileName, contentType, content);
    }

    private static IFormFile CreateFile(string fileName, string contentType, byte[] content)
    {
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
            DatumUpita = DateTime.UtcNow,
            DatumZadnjeAktivnosti = DateTime.UtcNow,
            Status = StatusUpita.CekaPodrsku,
            Poruke = [new PorukaPodrske { PosiljalacId = user.Id, TipAutora = TipAutoraPorukePodrske.Korisnik, Sadrzaj = "Test upit", DatumSlanja = DateTime.UtcNow }]
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

    private static async Task AddPurchasedVehicleAsync(ApplicationDbContext db, ApplicationUser user, Vozilo vehicle)
    {
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
            VoziloID = vehicle.VoziloID,
            Kolicina = 1,
            CijenaStavke = vehicle.Cijena ?? 0,
            NarudzbaID = order.NarudzbaID
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
            EmailSender = new FakeEmailSender();
            Environment = new TestWebHostEnvironment();
        }

        public IServiceProvider Provider { get; }
        public ApplicationDbContext Db { get; }
        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }
        public SignInManager<ApplicationUser> SignInManager { get; }
        public FakeEmailSender EmailSender { get; }
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

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<PasswordResetMessage> Messages { get; } = new();

        public Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink, DateTime expiresAtUtc)
        {
            Messages.Add(new PasswordResetMessage(toEmail, displayName, resetLink, expiresAtUtc));
            return Task.CompletedTask;
        }

        public Task SendSupportReplyEmailAsync(
            string toEmail,
            string displayName,
            string subject,
            string message,
            string detailsUrl,
            string culture) => Task.CompletedTask;
    }

    private sealed record PasswordResetMessage(
        string ToEmail,
        string DisplayName,
        string ResetLink,
        DateTime ExpiresAtUtc);

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

            var url = $"/{controller}/{actionContext.Action}";
            if (actionContext.Values == null)
            {
                return url;
            }

            var query = actionContext.Values
                .GetType()
                .GetProperties()
                .Select(property => new
                {
                    property.Name,
                    Value = property.GetValue(actionContext.Values)?.ToString()
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value!)}");

            var queryString = string.Join("&", query);
            return string.IsNullOrWhiteSpace(queryString) ? url : $"{url}?{queryString}";
        }

        public string Content(string contentPath) => contentPath;
        public bool IsLocalUrl(string url) => true;
        public string Link(string routeName, object values) => routeName;
        public string RouteUrl(UrlRouteContext routeContext) => routeContext.RouteName ?? "/";
    }
}
