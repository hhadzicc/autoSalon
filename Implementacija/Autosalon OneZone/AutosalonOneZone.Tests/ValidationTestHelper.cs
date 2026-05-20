using System.ComponentModel.DataAnnotations;

namespace AutosalonOneZone.Tests;

internal static class ValidationTestHelper
{
    public static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    public static bool IsValid(object model) => Validate(model).Count == 0;

    public static bool HasErrorFor(object model, string propertyName)
    {
        return Validate(model).Any(error => error.MemberNames.Contains(propertyName));
    }

    public static bool HasErrorMessageFor(object model, string propertyName, string errorMessage)
    {
        return Validate(model).Any(error =>
            error.MemberNames.Contains(propertyName) &&
            error.ErrorMessage == errorMessage);
    }
}
