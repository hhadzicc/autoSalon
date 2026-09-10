using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface ICheckoutService
{
    Task<CheckoutResult> PurchaseVehicleAsync(
        ApplicationUser user,
        int vehicleId,
        string ownerName,
        string expirationDate,
        PaymentFormValidationResult paymentForm);

    Task<CheckoutResult> PurchaseCartItemsAsync(
        ApplicationUser user,
        IReadOnlySet<int> vehicleIds,
        string ownerName,
        string expirationDate,
        PaymentFormValidationResult paymentForm);
}

public sealed class CheckoutService : ICheckoutService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<CheckoutService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<CheckoutResult> PurchaseVehicleAsync(
        ApplicationUser user,
        int vehicleId,
        string ownerName,
        string expirationDate,
        PaymentFormValidationResult paymentForm)
    {
        var vehicle = await _context.Vozila.FindAsync(vehicleId);
        if (vehicle == null)
        {
            return CheckoutResult.Failure(CheckoutStatus.VehicleNotFound);
        }

        var price = vehicle.Cijena ?? 0;
        if (price <= 0)
        {
            return CheckoutResult.Failure(CheckoutStatus.InvalidVehiclePrice);
        }

        var paymentResult = await _paymentService.ProcessPaymentAsync(new PaymentRequest
        {
            CardNumber = paymentForm.CleanCardNumber,
            ExpirationMonth = paymentForm.ExpirationMonth,
            ExpirationYear = paymentForm.ExpirationYear,
            Cvv = paymentForm.CleanCvv,
            Amount = price,
            CustomerName = ownerName.Trim(),
            Email = user.Email,
            Description = $"Purchase of {vehicle.Marka} {vehicle.Model}",
            ProductId = vehicleId
        });

        if (!paymentResult.Success)
        {
            return CheckoutResult.PaymentFailure(paymentResult.Message, price);
        }

        var order = new Narudzba
        {
            KorisnikId = user.Id,
            DatumNarudzbe = DateTime.Now,
            UkupnaCijena = price,
            Status = StatusNarudzbe.Placena
        };

        _context.Narudzbe.Add(order);
        await _context.SaveChangesAsync();

        _context.StavkeKorpe.Add(new StavkaKorpe
        {
            VoziloID = vehicleId,
            Kolicina = 1,
            CijenaStavke = price,
            NarudzbaID = order.NarudzbaID
        });
        _context.Placanja.Add(new Placanje
        {
            NarudzbaID = order.NarudzbaID,
            DatumPlacanja = DateTime.Now,
            Iznos = price,
            Status = StatusPlacanja.Uspjesno,
            KarticaID = null
        });

        var cart = await _context.Korpe
            .Include(k => k.StavkeKorpe)
            .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);
        var cartItem = cart?.StavkeKorpe.FirstOrDefault(item => item.VoziloID == vehicleId);
        if (cartItem != null)
        {
            _context.StavkeKorpe.Remove(cartItem);
            cart!.UkupnaCijena = Math.Max(0, cart.UkupnaCijena - cartItem.CijenaStavke);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Uspjesno izvrseno placanje za vozilo ID: {VoziloID}, iznos: {Iznos}, korisnik: {KorisnikId}",
            vehicleId,
            price,
            user.Id);

        return CheckoutResult.Success(order.NarudzbaID, price, 1);
    }

    public async Task<CheckoutResult> PurchaseCartItemsAsync(
        ApplicationUser user,
        IReadOnlySet<int> vehicleIds,
        string ownerName,
        string expirationDate,
        PaymentFormValidationResult paymentForm)
    {
        var cart = await _context.Korpe
            .Include(k => k.StavkeKorpe)
            .ThenInclude(item => item.Vozilo)
            .FirstOrDefaultAsync(k => k.KorisnikId == user.Id);

        if (cart == null)
        {
            return CheckoutResult.Failure(CheckoutStatus.CartNotFound);
        }

        if (vehicleIds.Any(vehicleId => cart.StavkeKorpe.All(item => item.VoziloID != vehicleId)))
        {
            return CheckoutResult.Failure(CheckoutStatus.VehicleMissingFromCart);
        }

        var selectedItems = cart.StavkeKorpe
            .Where(item => vehicleIds.Contains(item.VoziloID))
            .ToList();
        var totalPrice = selectedItems.Sum(item => item.CijenaStavke);

        var paymentResult = await _paymentService.ProcessPaymentAsync(new PaymentRequest
        {
            CardNumber = paymentForm.CleanCardNumber,
            ExpirationMonth = paymentForm.ExpirationMonth,
            ExpirationYear = paymentForm.ExpirationYear,
            Cvv = paymentForm.CleanCvv,
            Amount = totalPrice,
            CustomerName = ownerName,
            Email = user.Email,
            Description = $"Grupna kupovina {vehicleIds.Count} vozila",
            ProductId = 0
        });

        if (!paymentResult.Success)
        {
            return CheckoutResult.PaymentFailure(paymentResult.Message, totalPrice);
        }

        var order = new Narudzba
        {
            KorisnikId = user.Id,
            DatumNarudzbe = DateTime.Now,
            UkupnaCijena = totalPrice,
            Status = StatusNarudzbe.Placena
        };
        var card = new Kartica
        {
            BrojKartice = MaskCardNumber(paymentForm.CleanCardNumber),
            DatumIsteka = expirationDate,
            ImeVlasnika = ownerName,
            Cvv = "***"
        };

        _context.Narudzbe.Add(order);
        _context.Kartice.Add(card);
        await _context.SaveChangesAsync();

        _context.Placanja.Add(new Placanje
        {
            NarudzbaID = order.NarudzbaID,
            DatumPlacanja = DateTime.Now,
            Iznos = totalPrice,
            Status = StatusPlacanja.Uspjesno,
            KarticaID = card.KarticaID,
            KreditID = null
        });

        foreach (var item in selectedItems)
        {
            item.KorpaID = null;
            item.NarudzbaID = order.NarudzbaID;
            cart.UkupnaCijena -= item.CijenaStavke;
        }

        cart.UkupnaCijena = Math.Max(0, cart.UkupnaCijena);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Uspjesno grupno placanje za {BrojVozila} vozila, iznos: {Iznos}, korisnik: {KorisnikId}",
            selectedItems.Count,
            totalPrice,
            user.Id);

        return CheckoutResult.Success(order.NarudzbaID, totalPrice, selectedItems.Count);
    }

    private static string MaskCardNumber(string cardNumber) =>
        cardNumber.Length < 4
            ? cardNumber
            : cardNumber[^4..].PadLeft(cardNumber.Length, '*');
}

public enum CheckoutStatus
{
    Success,
    VehicleNotFound,
    InvalidVehiclePrice,
    CartNotFound,
    VehicleMissingFromCart,
    PaymentFailed
}

public sealed record CheckoutResult(
    CheckoutStatus Status,
    int? OrderId = null,
    decimal Amount = 0,
    int PurchasedCount = 0,
    string? PaymentMessage = null)
{
    public bool IsSuccess => Status == CheckoutStatus.Success;

    public static CheckoutResult Success(int orderId, decimal amount, int purchasedCount) =>
        new(CheckoutStatus.Success, orderId, amount, purchasedCount);

    public static CheckoutResult Failure(CheckoutStatus status) => new(status);

    public static CheckoutResult PaymentFailure(string message, decimal amount) =>
        new(CheckoutStatus.PaymentFailed, Amount: amount, PaymentMessage: message);
}
