using Microsoft.Extensions.Options;
using SkiaSharp;

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
    private const int MaximumSourceWidth = 4096;
    private const int MaximumSourceHeight = 4096;
    private const long MaximumSourcePixels = 16_000_000;
    private const int DetailWidth = 1600;
    private const int DetailHeight = 1000;
    private const int DetailQuality = 83;
    private const int ThumbnailWidth = 960;
    private const int ThumbnailHeight = 600;
    private const int ThumbnailQuality = 78;

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
        if (file.Length <= 0 || file.Length > 2 * 1024 * 1024)
        {
            throw new VehicleImageProcessingException();
        }

        var imageId = Guid.NewGuid().ToString("N");
        var detailFileName = $"{imageId}.webp";
        var thumbnailFileName = VehicleImageNames.ThumbnailFor(detailFileName);
        var detailPath = Path.Combine(_uploadsPath, detailFileName);
        var thumbnailPath = Path.Combine(_uploadsPath, thumbnailFileName);
        var detailTemporaryPath = $"{detailPath}.tmp";
        var thumbnailTemporaryPath = $"{thumbnailPath}.tmp";

        try
        {
            await using var sourceStream = file.OpenReadStream();
            using var sourceBuffer = new MemoryStream();
            await sourceStream.CopyToAsync(sourceBuffer, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            using var imageData = SKData.CreateCopy(sourceBuffer.ToArray());
            using var codec = SKCodec.Create(imageData);
            if (codec == null ||
                codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
                codec.Info.Width > MaximumSourceWidth || codec.Info.Height > MaximumSourceHeight ||
                (long)codec.Info.Width * codec.Info.Height > MaximumSourcePixels)
            {
                throw new VehicleImageProcessingException();
            }

            using var sourceImage = SKBitmap.Decode(imageData);
            if (sourceImage == null)
            {
                throw new VehicleImageProcessingException();
            }

            using var detailImage = Resize(sourceImage, DetailWidth, DetailHeight);
            using var thumbnailImage = Resize(detailImage, ThumbnailWidth, ThumbnailHeight);

            await SaveWebpAsync(detailImage, detailTemporaryPath, DetailQuality, cancellationToken);
            await SaveWebpAsync(thumbnailImage, thumbnailTemporaryPath, ThumbnailQuality, cancellationToken);
            File.Move(detailTemporaryPath, detailPath);
            File.Move(thumbnailTemporaryPath, thumbnailPath);
            return detailFileName;
        }
        catch (VehicleImageProcessingException)
        {
            DeleteIfExists(detailTemporaryPath);
            DeleteIfExists(thumbnailTemporaryPath);
            DeleteIfExists(detailPath);
            DeleteIfExists(thumbnailPath);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            DeleteIfExists(detailTemporaryPath);
            DeleteIfExists(thumbnailTemporaryPath);
            DeleteIfExists(detailPath);
            DeleteIfExists(thumbnailPath);
            throw new VehicleImageProcessingException(exception);
        }
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
        DeleteIfExists(path);

        var thumbnailFileName = VehicleImageNames.ThumbnailFor(safeFileName);
        DeleteIfExists(Path.Combine(_uploadsPath, thumbnailFileName));

        return Task.CompletedTask;
    }

    private static async Task SaveWebpAsync(
        SKBitmap bitmap,
        string path,
        int quality,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);
        if (encoded == null)
        {
            throw new VehicleImageProcessingException();
        }

        encoded.SaveTo(stream);
        await stream.FlushAsync(cancellationToken);
    }

    private static SKBitmap Resize(SKBitmap source, int maximumWidth, int maximumHeight)
    {
        var scale = Math.Min(
            1d,
            Math.Min((double)maximumWidth / source.Width, (double)maximumHeight / source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (!source.ScalePixels(
                resized,
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)))
        {
            resized.Dispose();
            throw new VehicleImageProcessingException();
        }

        return resized;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
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

public static class VehicleImageNames
{
    public static string ThumbnailFor(string detailFileName) =>
        $"{Path.GetFileNameWithoutExtension(detailFileName)}-thumb.webp";
}

public sealed class VehicleImageProcessingException : Exception
{
    public VehicleImageProcessingException()
    {
    }

    public VehicleImageProcessingException(Exception innerException)
        : base("The vehicle image could not be processed.", innerException)
    {
    }
}
