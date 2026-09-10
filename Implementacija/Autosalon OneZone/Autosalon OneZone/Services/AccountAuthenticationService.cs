using Autosalon_OneZone.Models;
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

    public AccountAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
            return AccountSignInStatus.InvalidCredentials;
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            password,
            rememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return AccountSignInStatus.Succeeded;
        }

        if (result.RequiresTwoFactor)
        {
            return AccountSignInStatus.RequiresTwoFactor;
        }

        if (result.IsLockedOut)
        {
            return AccountSignInStatus.LockedOut;
        }

        return result.IsNotAllowed
            ? AccountSignInStatus.NotAllowed
            : AccountSignInStatus.InvalidCredentials;
    }

    public Task SignOutAsync() => _signInManager.SignOutAsync();
}

public enum AccountSignInStatus
{
    Succeeded,
    InvalidCredentials,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed
}
