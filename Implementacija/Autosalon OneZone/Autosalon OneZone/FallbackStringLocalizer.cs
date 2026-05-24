using Microsoft.Extensions.Localization;

namespace Autosalon_OneZone
{
    public sealed class FallbackStringLocalizer<T> : IStringLocalizer<T>
    {
        private static readonly Dictionary<string, string> Values = new()
        {
            ["PasswordPolicyError"] = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.",
            ["Validation.MessageMaxLength"] = "Poruka može imati najviše 5000 znakova.",
            ["CommentMaxLength"] = "Komentar može imati najviše 1000 znakova.",
            ["UserSavedSuccess"] = "Korisnik uspješno sačuvan.",
            ["UserDeletedSuccess"] = "Korisnik uspješno obrisan.",
            ["VehicleNotFound"] = "Vozilo nije pronađeno.",
            ["UserNotFound"] = "Korisnik nije pronađen.",
            ["CartNotFound"] = "Korpa nije pronađena.",
            ["CartAddVehicleSuccess"] = "Vozilo je uspješno dodato u korpu.",
            ["CartVehicleAlreadyAdded"] = "Vozilo je već dodato u korpu.",
            ["CartAddError"] = "Došlo je do greške pri dodavanju vozila u korpu. Molimo pokušajte ponovo.",
            ["CartDisplayError"] = "Došlo je do greške pri prikazivanju korpe. Molimo pokušajte ponovo.",
            ["CartRemoveSuccess"] = "Vozilo je uklonjeno iz korpe.",
            ["CartVehicleNotFoundInCart"] = "Vozilo nije pronađeno u korpi.",
            ["CartRemoveError"] = "Došlo je do greške pri uklanjanju vozila iz korpe. Molimo pokušajte ponovo.",
            ["CartClearedSuccess"] = "Korpa je uspješno očišćena.",
            ["CartClearError"] = "Došlo je do greške pri čišćenju korpe.",
            ["PaymentNameRequired"] = "Ime vlasnika je obavezno.",
            ["PaymentNameFullRequired"] = "Unesite i ime i prezime.",
            ["PaymentCardRequired"] = "Broj kartice je obavezan.",
            ["PaymentCardLength"] = "Broj kartice mora sadržavati tačno 16 cifara.",
            ["PaymentExpiryRequired"] = "Datum isteka je obavezan.",
            ["PaymentExpiryFormat"] = "Datum isteka mora biti u formatu MM/YY.",
            ["PaymentExpiryMonth"] = "Mjesec mora biti između 01 i 12.",
            ["PaymentExpiryYear"] = "Godina isteka nije validna.",
            ["PaymentCardExpired"] = "Kartica je istekla.",
            ["PaymentCvvRequired"] = "CVV kod je obavezan.",
            ["PaymentCvvLength"] = "CVV kod mora sadržavati tačno 3 cifre.",
            ["InvalidFuelValue"] = "Odabrana vrijednost za gorivo nije validna.",
            ["InvalidImageContentError"] = "Uploadovani fajl mora biti validna slika.",
            ["PaymentProcessingError"] = "Došlo je do greške prilikom obrade plaćanja. Molimo pokušajte ponovo.",
            ["PaymentOrderRecorded"] = "Plaćanje je uspješno izvršeno. Vaša narudžba je evidentirana.",
            ["NoVehiclesSelectedForPurchase"] = "Odaberite najmanje jedno vozilo za kupovinu.",
            ["SelectedVehicleMissingFromCart"] = "Jedno ili više odabranih vozila nije pronađeno u vašoj korpi.",
            ["MultiVehiclePurchaseSuccess"] = "Uspješno ste kupili {0} vozila.",
            ["VehiclePriceInvalid"] = "Cijena vozila nije validna.",
            ["CommonError"] = "Došlo je do greške. Molimo pokušajte ponovo.",
            ["PasswordResetEmailSubject"] = "Resetovanje lozinke - Autosalon OneZone",
            ["PasswordResetEmailTitle"] = "Resetovanje lozinke",
            ["PasswordResetEmailHeroText"] = "Za vaš nalog pripremili smo siguran link za postavljanje nove lozinke.",
            ["PasswordResetEmailGreetingNameFallback"] = "korisniče",
            ["PasswordResetEmailGreeting"] = "Poštovani/a {0},",
            ["PasswordResetEmailIntro"] = "Zaprimili smo zahtjev za resetovanje lozinke na vašem Autosalon OneZone nalogu. Kliknite na dugme ispod kako biste postavili novu lozinku.",
            ["PasswordResetEmailValidityTitle"] = "Važenje linka",
            ["PasswordResetEmailValidityText"] = "Link ističe za 30 minuta, odnosno do {0}.",
            ["PasswordResetEmailButton"] = "Resetuj lozinku",
            ["PasswordResetEmailFallbackLink"] = "Ako dugme ne radi, kopirajte i otvorite sljedeći link:",
            ["PasswordResetEmailIgnore"] = "Ako niste zatražili resetovanje lozinke, slobodno zanemarite ovu poruku.",
            ["PasswordResetEmailFooter"] = "Ova poruka je automatski generisana iz Autosalon OneZone aplikacije.",
            ["PasswordResetEmailTextTitle"] = "Autosalon OneZone - resetovanje lozinke"
        };

        public LocalizedString this[string name] =>
            Values.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            Values.TryGetValue(name, out var value)
                ? new LocalizedString(name, string.Format(value, arguments), resourceNotFound: false)
                : new LocalizedString(name, string.Format(name, arguments), resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Enumerable.Empty<LocalizedString>();
        }
    }
}
