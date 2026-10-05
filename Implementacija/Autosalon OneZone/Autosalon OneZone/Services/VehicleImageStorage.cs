using Microsoft.Extensions.Options;

namespace Autosalon_OneZone.Services;

public sealed class VehicleImageStorageOptions
{
    public string UploadsPath { get; set; } = "App_Data/VehicleUploads";
}

public interface IVehicleImageStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<VehicleImageFile?> OpenReadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? fileName, CancellationToken cancellationToken = default);
}

public sealed class VehicleImageStorage : IVehicleImageStorage
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".webp"] = "image/webp"
        };

    private readonly string _seedImagesPath;
    private readonly string _uploadsPath;

    public VehicleImageStorage(
        IWebHostEnvironment environment,
        IOptions<VehicleImageStorageOptions> options)
    {
        _seedImagesPath = Path.Combine(environment.WebRootPath, "images", "vozila");
        _uploadsPath = Path.GetFullPath(
            Path.IsPathRooted(options.Value.UploadsPath)
                ? options.Value.UploadsPath
                : Path.Combine(environment.ContentRootPath, options.Value.UploadsPath));

        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ContentTypes.ContainsKey(extension))
        {
            throw new InvalidOperationException("Unsupported vehicle image extension.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_uploadsPath, fileName);
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await file.CopyToAsync(stream, cancellationToken);
        return fileName;
    }

    public Task<VehicleImageFile?> OpenReadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeFileName = SafeFileName(fileName);
        if (safeFileName == null)
        {
            return Task.FromResult<VehicleImageFile?>(null);
        }

        var extension = Path.GetExtension(safeFileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            return Task.FromResult<VehicleImageFile?>(null);
        }

        var root = IsSeedImage(safeFileName) ? _seedImagesPath : _uploadsPath;
        var path = Path.Combine(root, safeFileName);
        if (!File.Exists(path))
        {
            return Task.FromResult<VehicleImageFile?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<VehicleImageFile?>(new VehicleImageFile(stream, contentType));
    }

    public Task DeleteAsync(string? fileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeFileName = SafeFileName(fileName);
        if (safeFileName == null || IsSeedImage(safeFileName))
        {
            return Task.CompletedTask;
        }

        var path = Path.Combine(_uploadsPath, safeFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static bool IsSeedImage(string fileName) =>
        fileName.StartsWith("seed-", StringComparison.OrdinalIgnoreCase);

    private static string? SafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var safeFileName = Path.GetFileName(fileName);
        return string.Equals(fileName, safeFileName, StringComparison.Ordinal) &&
               ContentTypes.ContainsKey(Path.GetExtension(safeFileName))
            ? safeFileName
            : null;
    }
}

public sealed record VehicleImageFile(Stream Stream, string ContentType);
