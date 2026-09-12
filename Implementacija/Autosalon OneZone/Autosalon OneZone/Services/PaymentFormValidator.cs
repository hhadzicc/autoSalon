using Microsoft.Extensions.Localization;

namespace Autosalon_OneZone.Services;

public static class PaymentFormValidator
{
    public static PaymentFormValidationResult Validate(
        string? ownerName,
        string? cardNumber,
        string? expirationDate,
        string? cvv,
        IStringLocalizer<SharedResource> localizer,
        string fieldPrefix = "",
        DateTime? currentDate = null)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            errors.Add(FieldName(fieldPrefix, "imeVlasnika"), localizer["PaymentNameRequired"].Value);
        }

        var cleanCardNumber = DigitsOnly(cardNumber);
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            errors.Add(FieldName(fieldPrefix, "brojKartice"), localizer["PaymentCardRequired"].Value);
        }
        else if (cleanCardNumber.Length != 16)
        {
            errors.Add(FieldName(fieldPrefix, "brojKartice"), localizer["PaymentCardLength"].Value);
        }

        var expirationMonth = string.Empty;
        var expirationYear = string.Empty;
        ValidateExpirationDate(
            expirationDate,
            currentDate ?? DateTime.Now,
            localizer,
            FieldName(fieldPrefix, "datumIsteka"),
            errors,
            out expirationMonth,
            out expirationYear);

        var cleanCvv = cvv?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cvv))
        {
            errors.Add(FieldName(fieldPrefix, "cvv"), localizer["PaymentCvvRequired"].Value);
        }
        else if (cleanCvv.Length is < 3 or > 4 || !cleanCvv.All(char.IsDigit))
        {
            errors.Add(FieldName(fieldPrefix, "cvv"), localizer["PaymentCvvLength"].Value);
        }

        return new PaymentFormValidationResult(
            errors,
            cleanCardNumber,
            expirationMonth,
            expirationYear,
            cleanCvv);
    }

    private static void ValidateExpirationDate(
        string? expirationDate,
        DateTime currentDate,
        IStringLocalizer<SharedResource> localizer,
        string fieldName,
        IDictionary<string, string> errors,
        out string expirationMonth,
        out string expirationYear)
    {
        expirationMonth = string.Empty;
        expirationYear = string.Empty;

        if (string.IsNullOrWhiteSpace(expirationDate))
        {
            errors.Add(fieldName, localizer["PaymentExpiryRequired"].Value);
            return;
        }

        var dateParts = expirationDate.Split('/');
        if (dateParts.Length != 2)
        {
            errors.Add(fieldName, localizer["PaymentExpiryFormat"].Value);
            return;
        }

        expirationMonth = dateParts[0];
        expirationYear = "20" + dateParts[1];

        if (!int.TryParse(dateParts[0], out var month) || month is < 1 or > 12)
        {
            errors.Add(fieldName, localizer["PaymentExpiryMonth"].Value);
            return;
        }

        if (!int.TryParse(dateParts[1], out var shortYear))
        {
            errors.Add(fieldName, localizer["PaymentExpiryYear"].Value);
            return;
        }

        var fullYear = 2000 + shortYear;
        if (fullYear < currentDate.Year || (fullYear == currentDate.Year && month < currentDate.Month))
        {
            errors.Add(fieldName, localizer["PaymentCardExpired"].Value);
        }
    }

    private static string DigitsOnly(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    private static string FieldName(string prefix, string baseName) =>
        string.IsNullOrEmpty(prefix)
            ? baseName
            : prefix + char.ToUpperInvariant(baseName[0]) + baseName[1..];
}

public sealed record PaymentFormValidationResult(
    Dictionary<string, string> Errors,
    string CleanCardNumber,
    string ExpirationMonth,
    string ExpirationYear,
    string CleanCvv)
{
    public bool IsValid => Errors.Count == 0;
}
