using Autosalon_OneZone;
using Autosalon_OneZone.Services;

namespace AutosalonOneZone.Tests;

public class PaymentFormValidatorTests
{
    private readonly FallbackStringLocalizer<SharedResource> _localizer = new();

    [Fact]
    public void Validate_NormalizesValidPaymentDetails()
    {
        var result = PaymentFormValidator.Validate(
            "Demo Kupac",
            "4242 4242 4242 4242",
            "12/30",
            "1 2 3",
            _localizer,
            currentDate: new DateTime(2026, 9, 1));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("4242424242424242", result.CleanCardNumber);
        Assert.Equal("12", result.ExpirationMonth);
        Assert.Equal("2030", result.ExpirationYear);
        Assert.Equal("123", result.CleanCvv);
    }

    [Fact]
    public void Validate_UsesCheckoutFieldNames()
    {
        var result = PaymentFormValidator.Validate(
            null,
            null,
            null,
            null,
            _localizer,
            "checkout",
            new DateTime(2026, 9, 1));

        Assert.False(result.IsValid);
        Assert.Equal(
            new[]
            {
                "checkoutImeVlasnika",
                "checkoutBrojKartice",
                "checkoutDatumIsteka",
                "checkoutCvv"
            },
            result.Errors.Keys);
    }

    [Fact]
    public void Validate_InvalidMonthReturnsOneExpiryError()
    {
        var result = PaymentFormValidator.Validate(
            "Demo Kupac",
            "4242424242424242",
            "xx/30",
            "123",
            _localizer,
            currentDate: new DateTime(2026, 9, 1));

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("datumIsteka", result.Errors.Keys);
    }
}
