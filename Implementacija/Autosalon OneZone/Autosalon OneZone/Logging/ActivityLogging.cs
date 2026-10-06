using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Logging;

public sealed class ActivityLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public string LogPath { get; set; } = "App_Data/Logs";
    public string? SeqUrl { get; set; }
    public string? SeqApiKey { get; set; }
    public long FileSizeLimitBytes { get; set; } = 52_428_800;
}

public sealed record AuditActor(
    string? UserId = null,
    string? UserName = null,
    string? Email = null,
    IReadOnlyCollection<string>? Roles = null);

public interface IAuditLogger
{
    void Success(
        string action,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null);

    void Failure(
        string action,
        string reason,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null);
}

public sealed class NullAuditLogger : IAuditLogger
{
    public static NullAuditLogger Instance { get; } = new();

    private NullAuditLogger()
    {
    }

    public void Success(
        string action,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null)
    {
    }

    public void Failure(
        string action,
        string reason,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null)
    {
    }
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(
        ILogger<AuditLogger> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Success(
        string action,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null) =>
        Write(action, "Success", null, targetType, targetId, details, actor);

    public void Failure(
        string action,
        string reason,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        AuditActor? actor = null) =>
        Write(action, "Failure", reason, targetType, targetId, details, actor);

    private void Write(
        string action,
        string outcome,
        string? reason,
        string? targetType,
        string? targetId,
        object? details,
        AuditActor? actorOverride)
    {
        var context = _httpContextAccessor.HttpContext;
        var principal = context?.User;
        var actor = actorOverride ?? new AuditActor(
            principal?.FindFirstValue(ClaimTypes.NameIdentifier),
            principal?.Identity?.Name,
            principal?.FindFirstValue(ClaimTypes.Email),
            principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray());
        var userAgent = context?.Request.Headers.UserAgent.ToString();
        var device = UserAgentClassifier.Classify(userAgent);

        _logger.LogInformation(
            "Audit {AuditAction} completed with {AuditOutcome} for {TargetType} {TargetId}. " +
            "Actor {ActorUserId} {ActorUserName} {ActorEmail} {ActorRoles}; client {ClientIp} {DeviceType} " +
            "{Browser} {OperatingSystem}; reason {FailureReason}; details {@AuditDetails}",
            action,
            outcome,
            targetType,
            targetId,
            actor.UserId,
            actor.UserName,
            actor.Email,
            actor.Roles ?? Array.Empty<string>(),
            context?.Connection.RemoteIpAddress?.ToString(),
            device.DeviceType,
            device.Browser,
            device.OperatingSystem,
            reason,
            AuditDataSanitizer.Sanitize(details));
    }
}

public sealed record ClientDevice(string DeviceType, string Browser, string OperatingSystem);

public static class UserAgentClassifier
{
    public static ClientDevice Classify(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new ClientDevice("Unknown", "Unknown", "Unknown");
        }

        var deviceType = Contains(userAgent, "iPad", "Tablet")
            ? "Tablet"
            : Contains(userAgent, "Mobile", "Android", "iPhone", "Windows Phone")
                ? "Mobile"
                : "Desktop";
        var browser = Contains(userAgent, "Edg/")
            ? "Edge"
            : Contains(userAgent, "OPR/", "Opera")
                ? "Opera"
                : Contains(userAgent, "Chrome/")
                    ? "Chrome"
                    : Contains(userAgent, "Firefox/")
                        ? "Firefox"
                        : Contains(userAgent, "Safari/")
                            ? "Safari"
                            : "Other";
        var operatingSystem = Contains(userAgent, "Windows")
            ? "Windows"
            : Contains(userAgent, "Android")
                ? "Android"
                : Contains(userAgent, "iPhone", "iPad", "iOS")
                    ? "iOS"
                    : Contains(userAgent, "Mac OS", "Macintosh")
                        ? "macOS"
                        : Contains(userAgent, "Linux")
                            ? "Linux"
                            : "Other";

        return new ClientDevice(deviceType, browser, operatingSystem);
    }

    private static bool Contains(string source, params string[] values) =>
        values.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
}

internal static class AuditDataSanitizer
{
    private static readonly string[] SensitiveKeyParts =
    {
        "password", "lozinka", "token", "secret", "apikey", "authorization",
        "cookie", "cardnumber", "brojkartice", "cvv", "datumisteka", "expiration"
    };

    public static object Sanitize(object? value)
    {
        if (value == null)
        {
            return new Dictionary<string, object?>();
        }

        var element = JsonSerializer.SerializeToElement(value);
        return SanitizeElement(element, null) ?? new Dictionary<string, object?>();
    }

    private static object? SanitizeElement(JsonElement element, string? propertyName)
    {
        if (propertyName != null &&
            !propertyName.Equals("PasswordChanged", StringComparison.OrdinalIgnoreCase) &&
            SensitiveKeyParts.Any(part =>
                propertyName.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            return "[REDACTED]";
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => SanitizeElement(property.Value, property.Name)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => SanitizeElement(item, propertyName))
                .ToList(),
            JsonValueKind.String => Truncate(element.GetString()),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => Truncate(element.ToString())
        };
    }

    private static string? Truncate(string? value) =>
        value is { Length: > 10_000 } ? value[..10_000] : value;
}
