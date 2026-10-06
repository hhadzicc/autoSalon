using System.Text;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Services;

public interface IPasswordRecoveryService
{
    Task<PasswordResetRequest?> CreateResetRequestAsync(string email);
    Task SendResetEmailAsync(PasswordResetRequest request, string resetLink);
    Task<PasswordResetResult> ResetPasswordAsync(string userId, string encodedCode, string password);
}

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordRecoveryService> _logger;
    private readonly IAuditLogger _audit;

    public PasswordRecoveryService(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<PasswordRecoveryService> logger,
        IAuditLogger? audit = null)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
        _audit = audit ?? NullAuditLogger.Instance;
    }

    public async Task<PasswordResetRequest?> CreateResetRequestAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            _audit.Success(
                "PasswordResetRequested",
                "User",
                details: new { Email = email.Trim(), AccountFound = false });
            return null;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var displayName = $"{user.Ime} {user.Prezime}".Trim();

        _audit.Success(
            "PasswordResetRequested",
            "User",
            user.Id,
            new { user.Email, user.UserName, AccountFound = true });

        return new PasswordResetRequest(
            user.Id,
            encodedToken,
            user.Email!,
            string.IsNullOrWhiteSpace(displayName) ? user.UserName ?? user.Email! : displayName);
    }

    public async Task SendResetEmailAsync(PasswordResetRequest request, string resetLink)
    {
        try
        {
            await _emailSender.SendPasswordResetEmailAsync(
                request.Email,
                request.DisplayName,
                resetLink,
                DateTime.UtcNow.AddMinutes(30));
            _audit.Success(
                "PasswordResetEmailRequested",
                "User",
                request.UserId,
                new { request.Email, request.DisplayName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email for {Email}.", request.Email);
            _audit.Failure(
                "PasswordResetEmailRequested",
                ex.GetType().Name,
                "User",
                request.UserId,
                new { request.Email, request.DisplayName });
        }
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        string userId,
        string encodedCode,
        string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return PasswordResetResult.UserNotFound();
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode));
        }
        catch (FormatException)
        {
            return PasswordResetResult.InvalidCode();
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, token, password);
        if (!resetResult.Succeeded)
        {
            return PasswordResetResult.IdentityFailure(resetResult.Errors);
        }

        _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);
        _audit.Success(
            "PasswordResetCompleted",
            "User",
            user.Id,
            new { user.UserName, user.Email });
        return PasswordResetResult.Success();
    }
}

public sealed record PasswordResetRequest(
    string UserId,
    string EncodedCode,
    string Email,
    string DisplayName);

public enum PasswordResetStatus
{
    Succeeded,
    UserNotFound,
    InvalidCode,
    IdentityFailure
}

public sealed record PasswordResetResult(
    PasswordResetStatus Status,
    IReadOnlyList<string> Errors)
{
    public static PasswordResetResult Success() =>
        new(PasswordResetStatus.Succeeded, Array.Empty<string>());

    public static PasswordResetResult UserNotFound() =>
        new(PasswordResetStatus.UserNotFound, Array.Empty<string>());

    public static PasswordResetResult InvalidCode() =>
        new(PasswordResetStatus.InvalidCode, Array.Empty<string>());

    public static PasswordResetResult IdentityFailure(IEnumerable<IdentityError> errors) =>
        new(
            PasswordResetStatus.IdentityFailure,
            errors.Select(error => error.Description).ToList());
}
