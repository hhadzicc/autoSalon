using Autosalon_OneZone.Models;
using Autosalon_OneZone.Logging;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.Services;

public interface IAccountAuthenticationService
{
    Task<AccountSignInStatus> SignInAsync(
        string loginIdentifier,
        string password,
        bool rememberMe);

    Task SignOutAsync();
}

public sealed class AccountAuthenticationService : IAccountAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLogger _audit;

    public AccountAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditLogger? audit = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit ?? NullAuditLogger.Instance;
    }

    public async Task<AccountSignInStatus> SignInAsync(
        string loginIdentifier,
        string password,
        bool rememberMe)
    {
        var identifier = loginIdentifier.Trim();
        var user = await _userManager.FindByEmailAsync(identifier);
        user ??= await _userManager.FindByNameAsync(identifier);

        if (user == null)
        {
            _audit.Failure(
                "UserLogin",
                "InvalidCredentials",
                "User",
                details: new { LoginIdentifier = identifier });
            return AccountSignInStatus.InvalidCredentials;
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            password,
            rememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            _audit.Success(
                "UserLogin",
                "User",
                user.Id,
                new { user.UserName, user.Email, RememberMe = rememberMe },
                new AuditActor(user.Id, user.UserName, user.Email, roles.ToArray()));
            return AccountSignInStatus.Succeeded;
        }

        var status = result.RequiresTwoFactor
            ? AccountSignInStatus.RequiresTwoFactor
            : result.IsLockedOut
                ? AccountSignInStatus.LockedOut
                : result.IsNotAllowed
                    ? AccountSignInStatus.NotAllowed
                    : AccountSignInStatus.InvalidCredentials;
        _audit.Failure(
            "UserLogin",
            status.ToString(),
            "User",
            user.Id,
            new { user.UserName, user.Email },
            new AuditActor(user.Id, user.UserName, user.Email));

        return status;
    }

    public async Task SignOutAsync()
    {
        _audit.Success("UserLogout", "User");
        await _signInManager.SignOutAsync();
    }
}

public enum AccountSignInStatus
{
    Succeeded,
    InvalidCredentials,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed
}
