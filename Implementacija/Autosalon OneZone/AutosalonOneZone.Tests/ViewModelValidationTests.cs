using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Autosalon_OneZone.Validation;
using Autosalon_OneZone.ViewModels;
using Autosalon_OneZone.ViewModels.Admin;

namespace AutosalonOneZone.Tests;

public class ViewModelValidationTests
{
    private const string PasswordPolicyMessage = PasswordPolicy.ErrorMessage;

    private static RegisterViewModel ValidRegister() => new()
    {
        Ime = "Hamza",
        Prezime = "Hodzic",
        UserName = "hamzah",
        Email = "hamza@example.com",
        Password = "Valid123",
        ConfirmPassword = "Valid123"
    };

    private static LoginViewModel ValidLogin() => new()
    {
        LoginIdentifier = "hamza@example.com",
        Password = "Valid123"
    };

    private static ChangePasswordViewModel ValidChangePassword() => new()
    {
        CurrentPassword = "Oldpass1",
        NewPassword = "Newpass1",
        ConfirmPassword = "Newpass1"
    };

    private static EditProfileViewModel ValidEditProfile() => new()
    {
        Ime = "Hamza",
        Prezime = "Hodzic",
        Email = "hamza@example.com",
        UserName = "hamzah"
    };

    private static AddProfilViewModel ValidAdminProfile() => new()
    {
        UserName = "seller1",
        Email = "seller@example.com",
        Password = "Valid123",
        ConfirmPassword = "Valid123",
        Ime = "Demo",
        Prezime = "Seller",
        OdabraneRole = ["Prodavac"]
    };

    private static KontaktViewModel ValidKontakt() => new()
    {
        Naslov = "Upit za vozilo",
        Sadrzaj = "Zanima me dostupnost vozila."
    };

    private static AddVoziloViewModel ValidVehicle() => new()
    {
        Marka = "Audi",
        Model = "A4",
        Godiste = 2020,
        Gorivo = "Dizel",
        Kubikaza = 2.0m,
        Boja = TipBoje.Bijela,
        Kilometraza = 76000,
        Cijena = 43900,
        Opis = "Uredan automobil."
    };

    public static IEnumerable<object[]> InvalidRegisterCases()
    {
        yield return ["missing username", Mutate(ValidRegister(), x => x.UserName = ""), nameof(RegisterViewModel.UserName)];
        yield return ["username with at sign", Mutate(ValidRegister(), x => x.UserName = "hamza@example"), nameof(RegisterViewModel.UserName)];
        yield return ["username with local letters", Mutate(ValidRegister(), x => x.UserName = "hamzaš"), nameof(RegisterViewModel.UserName)];
        yield return ["username with dash", Mutate(ValidRegister(), x => x.UserName = "hamza-h"), nameof(RegisterViewModel.UserName)];
        yield return ["username with space", Mutate(ValidRegister(), x => x.UserName = "hamza h"), nameof(RegisterViewModel.UserName)];
        yield return ["missing email", Mutate(ValidRegister(), x => x.Email = ""), nameof(RegisterViewModel.Email)];
        yield return ["bad email", Mutate(ValidRegister(), x => x.Email = "not-email"), nameof(RegisterViewModel.Email)];
        yield return ["email with local letters", Mutate(ValidRegister(), x => x.Email = "korisnik\u0161@example.com"), nameof(RegisterViewModel.Email)];
        yield return ["missing password", Mutate(ValidRegister(), x => x.Password = ""), nameof(RegisterViewModel.Password)];
        yield return ["short password", Mutate(ValidRegister(), x => x.Password = "Aa1"), nameof(RegisterViewModel.Password)];
        yield return ["password without digit", Mutate(ValidRegister(), x => x.Password = "ValidPass"), nameof(RegisterViewModel.Password)];
        yield return ["password without uppercase", Mutate(ValidRegister(), x => x.Password = "valid123"), nameof(RegisterViewModel.Password)];
        yield return ["password without lowercase", Mutate(ValidRegister(), x => x.Password = "VALID123"), nameof(RegisterViewModel.Password)];
        yield return ["missing confirmation", Mutate(ValidRegister(), x => x.ConfirmPassword = ""), nameof(RegisterViewModel.ConfirmPassword)];
        yield return ["confirmation mismatch", Mutate(ValidRegister(), x => x.ConfirmPassword = "Other123"), nameof(RegisterViewModel.ConfirmPassword)];
        yield return ["missing first name", Mutate(ValidRegister(), x => x.Ime = ""), nameof(RegisterViewModel.Ime)];
        yield return ["first name with digit", Mutate(ValidRegister(), x => x.Ime = "Hamza1"), nameof(RegisterViewModel.Ime)];
        yield return ["missing last name", Mutate(ValidRegister(), x => x.Prezime = ""), nameof(RegisterViewModel.Prezime)];
        yield return ["last name with digit", Mutate(ValidRegister(), x => x.Prezime = "Hodzic2"), nameof(RegisterViewModel.Prezime)];
        yield return ["long username", Mutate(ValidRegister(), x => x.UserName = new string('a', 101)), nameof(RegisterViewModel.UserName)];
        yield return ["long first name", Mutate(ValidRegister(), x => x.Ime = new string('a', 101)), nameof(RegisterViewModel.Ime)];
        yield return ["long last name", Mutate(ValidRegister(), x => x.Prezime = new string('a', 101)), nameof(RegisterViewModel.Prezime)];
        yield return ["overlong password", Mutate(ValidRegister(), x => x.Password = "Aa1" + new string('a', 100)), nameof(RegisterViewModel.Password)];
    }

    public static IEnumerable<object[]> ValidRegisterCases()
    {
        yield return ["simple", ValidRegister()];
        yield return ["uppercase username", Mutate(ValidRegister(), x => x.UserName = "HamzaH")];
        yield return ["numeric username", Mutate(ValidRegister(), x => x.UserName = "hamza123")];
        yield return ["spaced first name", Mutate(ValidRegister(), x => x.Ime = "Hamza Harun")];
        yield return ["spaced last name", Mutate(ValidRegister(), x => x.Prezime = "Hodzic Test")];
        yield return ["local letters in name", Mutate(ValidRegister(), x => { x.Ime = "Željko"; x.Prezime = "Hadžić"; })];
        yield return ["hyphenated name", Mutate(ValidRegister(), x => { x.Ime = "Ana-Marija"; x.Prezime = "Kovač"; })];
        yield return ["long valid password", Mutate(ValidRegister(), x => { x.Password = "StrongPass123"; x.ConfirmPassword = "StrongPass123"; })];
        yield return ["different email domain", Mutate(ValidRegister(), x => x.Email = "user@test.ba")];
        yield return ["max username", Mutate(ValidRegister(), x => x.UserName = new string('a', 100))];
    }

    public static IEnumerable<object[]> InvalidLoginCases()
    {
        yield return ["missing identifier", Mutate(ValidLogin(), x => x.LoginIdentifier = ""), nameof(LoginViewModel.LoginIdentifier)];
        yield return ["missing password", Mutate(ValidLogin(), x => x.Password = ""), nameof(LoginViewModel.Password)];
        yield return ["too long identifier", Mutate(ValidLogin(), x => x.LoginIdentifier = new string('a', 257)), nameof(LoginViewModel.LoginIdentifier)];
        yield return ["null identifier", Mutate(ValidLogin(), x => x.LoginIdentifier = null!), nameof(LoginViewModel.LoginIdentifier)];
        yield return ["null password", Mutate(ValidLogin(), x => x.Password = null!), nameof(LoginViewModel.Password)];
        yield return ["empty both", new LoginViewModel { LoginIdentifier = "", Password = "" }, nameof(LoginViewModel.LoginIdentifier)];
        yield return ["empty both password field", new LoginViewModel { LoginIdentifier = "", Password = "" }, nameof(LoginViewModel.Password)];
        yield return ["overlong email", Mutate(ValidLogin(), x => x.LoginIdentifier = new string('b', 300)), nameof(LoginViewModel.LoginIdentifier)];
    }

    public static IEnumerable<object[]> InvalidForgotPasswordCases()
    {
        yield return ["missing email", new ForgotPasswordViewModel { Email = "" }, nameof(ForgotPasswordViewModel.Email)];
        yield return ["bad email", new ForgotPasswordViewModel { Email = "not-email" }, nameof(ForgotPasswordViewModel.Email)];
        yield return ["email with local letters", new ForgotPasswordViewModel { Email = "korisnik\u0107@example.com" }, nameof(ForgotPasswordViewModel.Email)];
        yield return ["null email", new ForgotPasswordViewModel { Email = null! }, nameof(ForgotPasswordViewModel.Email)];
    }

    public static IEnumerable<object[]> InvalidChangePasswordCases()
    {
        yield return ["missing current", Mutate(ValidChangePassword(), x => x.CurrentPassword = ""), nameof(ChangePasswordViewModel.CurrentPassword)];
        yield return ["missing new", Mutate(ValidChangePassword(), x => x.NewPassword = ""), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["missing confirm", Mutate(ValidChangePassword(), x => x.ConfirmPassword = ""), nameof(ChangePasswordViewModel.ConfirmPassword)];
        yield return ["short new", Mutate(ValidChangePassword(), x => x.NewPassword = "Aa1"), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["without digit", Mutate(ValidChangePassword(), x => x.NewPassword = "Newpasss"), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["without uppercase", Mutate(ValidChangePassword(), x => x.NewPassword = "newpass1"), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["without lowercase", Mutate(ValidChangePassword(), x => x.NewPassword = "NEWPASS1"), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["mismatch", Mutate(ValidChangePassword(), x => x.ConfirmPassword = "Other123"), nameof(ChangePasswordViewModel.ConfirmPassword)];
        yield return ["null current", Mutate(ValidChangePassword(), x => x.CurrentPassword = null!), nameof(ChangePasswordViewModel.CurrentPassword)];
        yield return ["null new", Mutate(ValidChangePassword(), x => x.NewPassword = null!), nameof(ChangePasswordViewModel.NewPassword)];
        yield return ["null confirm", Mutate(ValidChangePassword(), x => x.ConfirmPassword = null!), nameof(ChangePasswordViewModel.ConfirmPassword)];
        yield return ["overlong new", Mutate(ValidChangePassword(), x => x.NewPassword = "Aa1" + new string('a', 100)), nameof(ChangePasswordViewModel.NewPassword)];
    }

    public static IEnumerable<object[]> InvalidEditProfileCases()
    {
        yield return ["missing first name", Mutate(ValidEditProfile(), x => x.Ime = ""), nameof(EditProfileViewModel.Ime)];
        yield return ["missing last name", Mutate(ValidEditProfile(), x => x.Prezime = ""), nameof(EditProfileViewModel.Prezime)];
        yield return ["missing email", Mutate(ValidEditProfile(), x => x.Email = ""), nameof(EditProfileViewModel.Email)];
        yield return ["bad email", Mutate(ValidEditProfile(), x => x.Email = "bad"), nameof(EditProfileViewModel.Email)];
        yield return ["email with local letters", Mutate(ValidEditProfile(), x => x.Email = "hamza\u0111@example.com"), nameof(EditProfileViewModel.Email)];
        yield return ["missing username", Mutate(ValidEditProfile(), x => x.UserName = ""), nameof(EditProfileViewModel.UserName)];
        yield return ["username with at sign", Mutate(ValidEditProfile(), x => x.UserName = "hamza@example"), nameof(EditProfileViewModel.UserName)];
        yield return ["username with local letters", Mutate(ValidEditProfile(), x => x.UserName = "hamzaš"), nameof(EditProfileViewModel.UserName)];
        yield return ["first name with digit", Mutate(ValidEditProfile(), x => x.Ime = "Hamza1"), nameof(EditProfileViewModel.Ime)];
        yield return ["last name with digit", Mutate(ValidEditProfile(), x => x.Prezime = "Hodzic2"), nameof(EditProfileViewModel.Prezime)];
        yield return ["long first name", Mutate(ValidEditProfile(), x => x.Ime = new string('a', 101)), nameof(EditProfileViewModel.Ime)];
        yield return ["long last name", Mutate(ValidEditProfile(), x => x.Prezime = new string('a', 101)), nameof(EditProfileViewModel.Prezime)];
        yield return ["long username", Mutate(ValidEditProfile(), x => x.UserName = new string('a', 101)), nameof(EditProfileViewModel.UserName)];
        yield return ["null email", Mutate(ValidEditProfile(), x => x.Email = null!), nameof(EditProfileViewModel.Email)];
        yield return ["null username", Mutate(ValidEditProfile(), x => x.UserName = null!), nameof(EditProfileViewModel.UserName)];
    }

    public static IEnumerable<object[]> InvalidAdminProfileCases()
    {
        yield return ["missing username", Mutate(ValidAdminProfile(), x => x.UserName = ""), nameof(AddProfilViewModel.UserName)];
        yield return ["username with at sign", Mutate(ValidAdminProfile(), x => x.UserName = "admin@example"), nameof(AddProfilViewModel.UserName)];
        yield return ["username with local letters", Mutate(ValidAdminProfile(), x => x.UserName = "prodavacš"), nameof(AddProfilViewModel.UserName)];
        yield return ["missing email", Mutate(ValidAdminProfile(), x => x.Email = ""), nameof(AddProfilViewModel.Email)];
        yield return ["bad email", Mutate(ValidAdminProfile(), x => x.Email = "bad"), nameof(AddProfilViewModel.Email)];
        yield return ["email with local letters", Mutate(ValidAdminProfile(), x => x.Email = "prodavac\u017e@example.com"), nameof(AddProfilViewModel.Email)];
        yield return ["short password", Mutate(ValidAdminProfile(), x => x.Password = "Aa1"), nameof(AddProfilViewModel.Password)];
        yield return ["without digit", Mutate(ValidAdminProfile(), x => x.Password = "ValidPass"), nameof(AddProfilViewModel.Password)];
        yield return ["without uppercase", Mutate(ValidAdminProfile(), x => x.Password = "valid123"), nameof(AddProfilViewModel.Password)];
        yield return ["without lowercase", Mutate(ValidAdminProfile(), x => x.Password = "VALID123"), nameof(AddProfilViewModel.Password)];
        yield return ["mismatch", Mutate(ValidAdminProfile(), x => x.ConfirmPassword = "Other123"), nameof(AddProfilViewModel.ConfirmPassword)];
        yield return ["overlong password", Mutate(ValidAdminProfile(), x => x.Password = "Aa1" + new string('a', 100)), nameof(AddProfilViewModel.Password)];
        yield return ["null username", Mutate(ValidAdminProfile(), x => x.UserName = null!), nameof(AddProfilViewModel.UserName)];
        yield return ["null email", Mutate(ValidAdminProfile(), x => x.Email = null!), nameof(AddProfilViewModel.Email)];
        yield return ["missing first name", Mutate(ValidAdminProfile(), x => x.Ime = ""), nameof(AddProfilViewModel.Ime)];
        yield return ["missing last name", Mutate(ValidAdminProfile(), x => x.Prezime = ""), nameof(AddProfilViewModel.Prezime)];
        yield return ["first name with digit", Mutate(ValidAdminProfile(), x => x.Ime = "Demo1"), nameof(AddProfilViewModel.Ime)];
        yield return ["last name with digit", Mutate(ValidAdminProfile(), x => x.Prezime = "Seller2"), nameof(AddProfilViewModel.Prezime)];
        yield return ["invalid optional edit password", Mutate(ValidAdminProfile(), x => { x.UserId = "1"; x.Password = "weak"; x.ConfirmPassword = "weak"; }), nameof(AddProfilViewModel.Password)];
        yield return ["valid optional edit empty confirm only mismatch", Mutate(ValidAdminProfile(), x => { x.UserId = "1"; x.Password = ""; x.ConfirmPassword = "Something123"; }), nameof(AddProfilViewModel.ConfirmPassword)];
        yield return ["too long password edit", Mutate(ValidAdminProfile(), x => { x.UserId = "1"; x.Password = "Aa1" + new string('a', 100); }), nameof(AddProfilViewModel.Password)];
    }

    public static IEnumerable<object[]> KontaktCases()
    {
        yield return ["missing title", new KontaktViewModel { Naslov = "", Sadrzaj = "Poruka" }, nameof(KontaktViewModel.Naslov)];
        yield return ["missing content", new KontaktViewModel { Naslov = "Naslov", Sadrzaj = "" }, nameof(KontaktViewModel.Sadrzaj)];
        yield return ["long title", new KontaktViewModel { Naslov = new string('a', 201), Sadrzaj = "Poruka" }, nameof(KontaktViewModel.Naslov)];
        yield return ["long content", new KontaktViewModel { Naslov = "Naslov", Sadrzaj = new string('a', 5001) }, nameof(KontaktViewModel.Sadrzaj)];
        yield return ["null title", new KontaktViewModel { Naslov = null!, Sadrzaj = "Poruka" }, nameof(KontaktViewModel.Naslov)];
        yield return ["null content", new KontaktViewModel { Naslov = "Naslov", Sadrzaj = null! }, nameof(KontaktViewModel.Sadrzaj)];
        yield return ["both empty title", new KontaktViewModel { Naslov = "", Sadrzaj = "" }, nameof(KontaktViewModel.Naslov)];
        yield return ["both empty content", new KontaktViewModel { Naslov = "", Sadrzaj = "" }, nameof(KontaktViewModel.Sadrzaj)];
    }

    public static IEnumerable<object[]> InvalidVehicleCases()
    {
        yield return ["missing brand", Mutate(ValidVehicle(), x => x.Marka = ""), nameof(AddVoziloViewModel.Marka)];
        yield return ["long brand", Mutate(ValidVehicle(), x => x.Marka = new string('a', 101)), nameof(AddVoziloViewModel.Marka)];
        yield return ["missing model", Mutate(ValidVehicle(), x => x.Model = ""), nameof(AddVoziloViewModel.Model)];
        yield return ["long model", Mutate(ValidVehicle(), x => x.Model = new string('a', 101)), nameof(AddVoziloViewModel.Model)];
        yield return ["missing year", Mutate(ValidVehicle(), x => x.Godiste = null), nameof(AddVoziloViewModel.Godiste)];
        yield return ["year too old", Mutate(ValidVehicle(), x => x.Godiste = 1899), nameof(AddVoziloViewModel.Godiste)];
        yield return ["year too new", Mutate(ValidVehicle(), x => x.Godiste = VehicleYearPolicy.MaximumYear + 1), nameof(AddVoziloViewModel.Godiste)];
        yield return ["missing fuel", Mutate(ValidVehicle(), x => x.Gorivo = ""), nameof(AddVoziloViewModel.Gorivo)];
        yield return ["invalid fuel", Mutate(ValidVehicle(), x => x.Gorivo = "Steam"), nameof(AddVoziloViewModel.Gorivo)];
        yield return ["missing engine", Mutate(ValidVehicle(), x => x.Kubikaza = null), nameof(AddVoziloViewModel.Kubikaza)];
        yield return ["negative engine", Mutate(ValidVehicle(), x => x.Kubikaza = 0), nameof(AddVoziloViewModel.Kubikaza)];
        yield return ["missing color", Mutate(ValidVehicle(), x => x.Boja = null), nameof(AddVoziloViewModel.Boja)];
        yield return ["invalid color", Mutate(ValidVehicle(), x => x.Boja = (TipBoje)999), nameof(AddVoziloViewModel.Boja)];
        yield return ["negative mileage", Mutate(ValidVehicle(), x => x.Kilometraza = -1), nameof(AddVoziloViewModel.Kilometraza)];
        yield return ["zero price", Mutate(ValidVehicle(), x => x.Cijena = 0), nameof(AddVoziloViewModel.Cijena)];
        yield return ["missing description", Mutate(ValidVehicle(), x => x.Opis = ""), nameof(AddVoziloViewModel.Opis)];
        yield return ["long description", Mutate(ValidVehicle(), x => x.Opis = new string('a', 2001)), nameof(AddVoziloViewModel.Opis)];
    }

    public static IEnumerable<object[]> DomainInvalidCases()
    {
        yield return ["empty application first name", new ApplicationUser { Ime = "", Prezime = "Test" }, nameof(ApplicationUser.Ime)];
        yield return ["empty application last name", new ApplicationUser { Ime = "Test", Prezime = "" }, nameof(ApplicationUser.Prezime)];
        yield return ["long application first name", new ApplicationUser { Ime = new string('a', 101), Prezime = "Test" }, nameof(ApplicationUser.Ime)];
        yield return ["cart missing user", new Korpa { KorisnikId = null! }, nameof(Korpa.KorisnikId)];
        yield return ["order missing user", new Narudzba { DatumNarudzbe = DateTime.UtcNow, Status = StatusNarudzbe.Kreirana }, nameof(Narudzba.KorisnikId)];
        yield return ["review low rating", new Recenzija { Ocjena = 0, DatumRecenzije = DateTime.UtcNow, KorisnikId = "u", VoziloID = 1 }, nameof(Recenzija.Ocjena)];
        yield return ["review high rating", new Recenzija { Ocjena = 6, DatumRecenzije = DateTime.UtcNow, KorisnikId = "u", VoziloID = 1 }, nameof(Recenzija.Ocjena)];
        yield return ["review long comment", new Recenzija { Ocjena = 4, Komentar = new string('a', 1001), DatumRecenzije = DateTime.UtcNow, KorisnikId = "u", VoziloID = 1 }, nameof(Recenzija.Komentar)];
        yield return ["support missing title", new Podrska { Naslov = "", Sadrzaj = "x", DatumUpita = DateTime.UtcNow, Status = StatusUpita.Poslat, KorisnikId = "u" }, nameof(Podrska.Naslov)];
        yield return ["support long title", new Podrska { Naslov = new string('a', 201), Sadrzaj = "x", DatumUpita = DateTime.UtcNow, Status = StatusUpita.Poslat, KorisnikId = "u" }, nameof(Podrska.Naslov)];
        yield return ["support missing content", new Podrska { Naslov = "x", Sadrzaj = "", DatumUpita = DateTime.UtcNow, Status = StatusUpita.Poslat, KorisnikId = "u" }, nameof(Podrska.Sadrzaj)];
        yield return ["card invalid number", new Kartica { BrojKartice = "abc", DatumIsteka = "12/30", Cvv = "123", ImeVlasnika = "Test" }, nameof(Kartica.BrojKartice)];
        yield return ["card missing owner", new Kartica { BrojKartice = "4242424242424242", DatumIsteka = "12/30", Cvv = "123", ImeVlasnika = "" }, nameof(Kartica.ImeVlasnika)];
    }

    [Theory]
    [MemberData(nameof(InvalidRegisterCases))]
    public void RegisterViewModel_rejects_invalid_values(string _, RegisterViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [InlineData("Aa1")]
    [InlineData("ValidPass")]
    [InlineData("valid123")]
    [InlineData("VALID123")]
    public void RegisterViewModel_uses_consistent_password_policy_message(string password)
    {
        var model = ValidRegister();
        model.Password = password;
        model.ConfirmPassword = password;

        Assert.True(ValidationTestHelper.HasErrorMessageFor(model, nameof(RegisterViewModel.Password), PasswordPolicyMessage));
    }

    [Theory]
    [MemberData(nameof(ValidRegisterCases))]
    public void RegisterViewModel_accepts_valid_values(string _, RegisterViewModel model)
    {
        Assert.True(ValidationTestHelper.IsValid(model));
    }

    [Theory]
    [MemberData(nameof(InvalidLoginCases))]
    public void LoginViewModel_rejects_invalid_values(string _, LoginViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [MemberData(nameof(InvalidForgotPasswordCases))]
    public void ForgotPasswordViewModel_rejects_invalid_values(string _, ForgotPasswordViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [MemberData(nameof(InvalidChangePasswordCases))]
    public void ChangePasswordViewModel_rejects_invalid_values(string _, ChangePasswordViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [InlineData("Aa1")]
    [InlineData("Newpasss")]
    [InlineData("newpass1")]
    [InlineData("NEWPASS1")]
    public void ChangePasswordViewModel_uses_consistent_password_policy_message(string password)
    {
        var model = ValidChangePassword();
        model.NewPassword = password;
        model.ConfirmPassword = password;

        Assert.True(ValidationTestHelper.HasErrorMessageFor(model, nameof(ChangePasswordViewModel.NewPassword), PasswordPolicyMessage));
    }

    [Theory]
    [InlineData("Oldpass1", "Newpass1", "Newpass1")]
    [InlineData("Oldpass1", "Password1", "Password1")]
    [InlineData("Oldpass1", "Strong123", "Strong123")]
    [InlineData("Oldpass1", "Aa123456", "Aa123456")]
    [InlineData("Current1", "NewValue1", "NewValue1")]
    [InlineData("Current1", "Another1", "Another1")]
    public void ChangePasswordViewModel_accepts_valid_policy_passwords(string current, string password, string confirm)
    {
        Assert.True(ValidationTestHelper.IsValid(new ChangePasswordViewModel
        {
            CurrentPassword = current,
            NewPassword = password,
            ConfirmPassword = confirm
        }));
    }

    [Theory]
    [MemberData(nameof(InvalidEditProfileCases))]
    public void EditProfileViewModel_rejects_invalid_values(string _, EditProfileViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [MemberData(nameof(InvalidAdminProfileCases))]
    public void AddProfilViewModel_rejects_invalid_values(string _, AddProfilViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [InlineData("Aa1")]
    [InlineData("ValidPass")]
    [InlineData("valid123")]
    [InlineData("VALID123")]
    public void AddProfilViewModel_uses_consistent_password_policy_message(string password)
    {
        var model = ValidAdminProfile();
        model.Password = password;
        model.ConfirmPassword = password;

        Assert.True(ValidationTestHelper.HasErrorMessageFor(model, nameof(AddProfilViewModel.Password), PasswordPolicyMessage));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Valid123", "Valid123")]
    public void AddProfilViewModel_allows_optional_password_for_existing_user(string? password, string? confirm)
    {
        var model = ValidAdminProfile();
        model.UserId = "existing";
        model.Password = password;
        model.ConfirmPassword = confirm;

        Assert.True(ValidationTestHelper.IsValid(model));
    }

    [Theory]
    [MemberData(nameof(KontaktCases))]
    public void KontaktViewModel_rejects_invalid_values(string _, KontaktViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Theory]
    [MemberData(nameof(InvalidVehicleCases))]
    public void AddVoziloViewModel_rejects_invalid_values(string _, AddVoziloViewModel model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    [Fact]
    public void AddVoziloViewModel_allows_electric_vehicle_without_displacement()
    {
        var model = ValidVehicle();
        model.Gorivo = "Elektro";
        model.Kubikaza = null;

        Assert.True(ValidationTestHelper.IsValid(model));
    }

    [Theory]
    [MemberData(nameof(DomainInvalidCases))]
    public void Domain_entities_reject_invalid_values(string _, object model, string member)
    {
        Assert.True(ValidationTestHelper.HasErrorFor(model, member));
    }

    private static T Mutate<T>(T model, Action<T> mutate)
    {
        mutate(model);
        return model;
    }
}
