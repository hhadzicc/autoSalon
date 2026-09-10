using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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

    Task<Narudzba?> GetOrderAsync(int orderId, string userId, bool canViewAllOrders);
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
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await AcquireVehicleLocksAsync(new[] { vehicleId }))
        {
            return CheckoutResult.Failure(CheckoutStatus.CheckoutBusy);
        }

        var vehicle = await _context.Vozila.FirstOrDefaultAsync(item => item.VoziloID == vehicleId);
        if (vehicle == null)
        {
            return CheckoutResult.Failure(CheckoutStatus.VehicleNotFound);
        }

        if (await IsPurchasedAsync(vehicleId))
        {
            return CheckoutResult.Failure(CheckoutStatus.VehicleAlreadyPurchased);
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
            ProductId = vehicleId,
            IdempotencyKey = BuildIdempotencyKey(
                user,
                new[] { vehicleId },
                price,
                ownerName,
                expirationDate,
                paymentForm.CleanCardNumber)
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

        await RemoveVehiclesFromCartsAsync(new[] { vehicleId });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
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
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await AcquireVehicleLocksAsync(vehicleIds))
        {
            return CheckoutResult.Failure(CheckoutStatus.CheckoutBusy);
        }

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

        if (await _context.StavkeKorpe.AsNoTracking().AnyAsync(item =>
                item.NarudzbaID != null && vehicleIds.Contains(item.VoziloID)))
        {
            return CheckoutResult.Failure(CheckoutStatus.VehicleAlreadyPurchased);
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
            ProductId = 0,
            IdempotencyKey = BuildIdempotencyKey(
                user,
                vehicleIds,
                totalPrice,
                ownerName,
                expirationDate,
                paymentForm.CleanCardNumber)
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

        await RemoveVehiclesFromCartsAsync(vehicleIds, selectedItems.Select(item => item.StavkaID));
        cart.UkupnaCijena = Math.Max(0, cart.UkupnaCijena);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Uspjesno grupno placanje za {BrojVozila} vozila, iznos: {Iznos}, korisnik: {KorisnikId}",
            selectedItems.Count,
            totalPrice,
            user.Id);

        return CheckoutResult.Success(order.NarudzbaID, totalPrice, selectedItems.Count);
    }

    public async Task<Narudzba?> GetOrderAsync(int orderId, string userId, bool canViewAllOrders)
    {
        var query = _context.Narudzbe
            .Include(order => order.StavkeKorpe)
            .ThenInclude(item => item.Vozilo)
            .AsQueryable();

        if (!canViewAllOrders)
        {
            query = query.Where(order => order.KorisnikId == userId);
        }

        return await query.FirstOrDefaultAsync(order => order.NarudzbaID == orderId);
    }

    private static string MaskCardNumber(string cardNumber) =>
        cardNumber.Length < 4
            ? cardNumber
            : cardNumber[^4..].PadLeft(cardNumber.Length, '*');

    private Task<bool> IsPurchasedAsync(int vehicleId) =>
        _context.StavkeKorpe.AsNoTracking().AnyAsync(item =>
            item.VoziloID == vehicleId && item.NarudzbaID != null);

    private async Task RemoveVehiclesFromCartsAsync(
        IEnumerable<int> vehicleIds,
        IEnumerable<int>? excludedCartItemIds = null)
    {
        var vehicleIdSet = vehicleIds.ToHashSet();
        var excludedItemIdSet = excludedCartItemIds?.ToHashSet() ?? new HashSet<int>();
        var query = _context.StavkeKorpe
            .Include(item => item.Korpa)
            .Where(item =>
                item.KorpaID != null &&
                vehicleIdSet.Contains(item.VoziloID));
        if (excludedItemIdSet.Count > 0)
        {
            query = query.Where(item => !excludedItemIdSet.Contains(item.StavkaID));
        }

        var cartItems = await query.ToListAsync();

        foreach (var item in cartItems)
        {
            if (item.Korpa != null)
            {
                item.Korpa.UkupnaCijena = Math.Max(
                    0,
                    item.Korpa.UkupnaCijena - item.CijenaStavke * item.Kolicina);
            }
        }

        _context.StavkeKorpe.RemoveRange(cartItems);
    }

    private async Task<bool> AcquireVehicleLocksAsync(IEnumerable<int> vehicleIds)
    {
        if (!_context.Database.IsSqlServer())
        {
            return true;
        }

        var transaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A database transaction is required before acquiring checkout locks.");
        var connection = _context.Database.GetDbConnection();

        foreach (var vehicleId in vehicleIds.Distinct().OrderBy(id => id))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;
                SELECT @result;
                """;

            var resource = command.CreateParameter();
            resource.ParameterName = "@resource";
            resource.Value = $"vehicle-purchase:{vehicleId}";
            command.Parameters.Add(resource);

            var result = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
            if (result < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildIdempotencyKey(
        ApplicationUser user,
        IEnumerable<int> vehicleIds,
        decimal amount,
        string ownerName,
        string expirationDate,
        string cardNumber)
    {
        var purchaseData = string.Join("|", new[]
        {
            user.Id,
            user.Email ?? string.Empty,
            string.Join(',', vehicleIds.OrderBy(id => id)),
            amount.ToString(CultureInfo.InvariantCulture),
            ownerName.Trim(),
            expirationDate.Trim(),
            cardNumber
        });
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(purchaseData));
        return $"checkout-{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}

public enum CheckoutStatus
{
    Success,
    VehicleNotFound,
    InvalidVehiclePrice,
    CartNotFound,
    VehicleMissingFromCart,
    VehicleAlreadyPurchased,
    CheckoutBusy,
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
