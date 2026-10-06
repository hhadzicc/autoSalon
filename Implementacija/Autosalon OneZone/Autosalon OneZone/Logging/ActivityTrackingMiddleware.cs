using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace Autosalon_OneZone.Logging;

public sealed class ActivityTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActivityTrackingMiddleware> _logger;

    public ActivityTrackingMiddleware(
        RequestDelegate next,
        ILogger<ActivityTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();
        var device = UserAgentClassifier.Classify(userAgent);
        var user = context.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user.Identity?.Name;
        var email = user.FindFirstValue(ClaimTypes.Email);
        var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var controller = context.Request.RouteValues["controller"]?.ToString();
        var action = context.Request.RouteValues["action"]?.ToString();
        var stopwatch = Stopwatch.StartNew();

        using var requestIdProperty = LogContext.PushProperty("RequestId", context.TraceIdentifier);
        using var userIdProperty = LogContext.PushProperty("ActorUserId", userId);
        using var userNameProperty = LogContext.PushProperty("ActorUserName", userName);
        using var emailProperty = LogContext.PushProperty("ActorEmail", email);
        using var rolesProperty = LogContext.PushProperty("ActorRoles", roles, destructureObjects: true);
        using var ipProperty = LogContext.PushProperty("ClientIp", clientIp);
        using var deviceProperty = LogContext.PushProperty("DeviceType", device.DeviceType);
        using var browserProperty = LogContext.PushProperty("Browser", device.Browser);
        using var osProperty = LogContext.PushProperty("OperatingSystem", device.OperatingSystem);

        try
        {
            await _next(context);
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {HttpMethod} {RequestPath} handled by {Controller}.{Action} returned {StatusCode} in {ElapsedMilliseconds} ms from {ClientIp} using {DeviceType} {Browser} on {OperatingSystem}. User agent {UserAgent}",
                context.Request.Method,
                path,
                controller,
                action,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                clientIp,
                device.DeviceType,
                device.Browser,
                device.OperatingSystem,
                Truncate(userAgent));
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "HTTP {HttpMethod} {RequestPath} handled by {Controller}.{Action} failed after {ElapsedMilliseconds} ms.",
                context.Request.Method,
                path,
                controller,
                action,
                stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private static bool ShouldSkip(string path) =>
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/vehicle-images/", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string value) =>
        value.Length > 512 ? value[..512] : value;
}
