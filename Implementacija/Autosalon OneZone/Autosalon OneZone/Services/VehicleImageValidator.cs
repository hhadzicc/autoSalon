using Microsoft.AspNetCore.Http;

namespace Autosalon_OneZone.Services;

public static class VehicleImageValidator
{
    private const long MaximumFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif", "image/bmp" };

    public static VehicleImageValidationResult Validate(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var hasAllowedExtension = AllowedExtensions.Contains(extension);
        var hasAllowedContentType = !string.IsNullOrWhiteSpace(file.ContentType) &&
                                    AllowedContentTypes.Contains(file.ContentType);
        var hasValidContent = hasAllowedContentType &&
                              (!hasAllowedExtension || HasValidSignature(file, extension.ToLowerInvariant()));

        return new VehicleImageValidationResult(
            hasAllowedExtension,
            hasValidContent,
            file.Length <= MaximumFileSize);
    }

    private static bool HasValidSignature(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[8];
        var bytesRead = stream.Read(header);

        return extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                  header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 &&
                      header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                      header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            ".gif" => bytesRead >= 6 &&
                      header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38 &&
                      (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61,
            ".bmp" => bytesRead >= 2 && header[0] == 0x42 && header[1] == 0x4D,
            _ => false
        };
    }
}

public sealed record VehicleImageValidationResult(
    bool HasAllowedExtension,
    bool HasValidContent,
    bool IsWithinSizeLimit);
