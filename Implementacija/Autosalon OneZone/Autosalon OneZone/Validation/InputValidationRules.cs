using System.Linq;

namespace Autosalon_OneZone.Validation
{
    public static class PasswordPolicy
    {
        public const int RequiredLength = 8;
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";
        public const string HtmlPattern = @"(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}";
        public const string ErrorMessage = "Lozinka mora imati najmanje 8 karaktera, jednu cifru, jedno malo i jedno veliko slovo.";

        public static bool IsValid(string? password)
        {
            return !string.IsNullOrWhiteSpace(password)
                && password.Length >= RequiredLength
                && password.Any(char.IsDigit)
                && password.Any(char.IsLower)
                && password.Any(char.IsUpper);
        }
    }

    public static class UserInputPatterns
    {
        public const string Email = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$";
        public const string EmailHtml = @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}";
        public const string Username = @"^[A-Za-z0-9]+$";
        public const string UsernameHtml = @"[A-Za-z0-9]+";
        public const string PersonName = @"^[A-Za-z\u00C0-\u024F]+(?:[ '’\-][A-Za-z\u00C0-\u024F]+)*$";
        public const string PersonNameHtml = @"[A-Za-z\u00C0-\u024F]+(?:[ '’\-][A-Za-z\u00C0-\u024F]+)*";
    }

    public static class VehicleYearPolicy
    {
        public const int MinimumYear = 1900;
        public static int MaximumYear => DateTime.UtcNow.Year;

        public static bool IsValid(int year)
        {
            return year >= MinimumYear && year <= MaximumYear;
        }
    }
}
