using Autosalon_OneZone.Models;
using Autosalon_OneZone.Logging;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Autosalon_OneZone.Services;

public interface IProfileAccountService
{
    EditProfileViewModel GetEditModel(ApplicationUser user);
    Task<ProfileUpdateResult> UpdateAsync(ApplicationUser user, EditProfileViewModel model);
    Task<IdentityOperationResult> ChangePasswordAsync(
        ApplicationUser user,
        string currentPassword,
        string newPassword);
}

public sealed class ProfileAccountService : IProfileAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuditLogger _audit;

    public ProfileAccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuditLogger? audit = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _audit = audit ?? NullAuditLogger.Instance;
    }

    public EditProfileViewModel GetEditModel(ApplicationUser user) => new()
    {
        Ime = user.Ime,
        Prezime = user.Prezime,
        Email = user.Email,
        UserName = user.UserName
    };

    public async Task<ProfileUpdateResult> UpdateAsync(ApplicationUser user, EditProfileViewModel model)
    {
        var previous = new
        {
            user.Ime,
            user.Prezime,
            user.Email,
            user.UserName
        };
        var firstNameChanged = user.Ime != model.Ime;
        var lastNameChanged = user.Prezime != model.Prezime;
        var emailChanged = user.Email != model.Email;
        var usernameChanged = user.UserName != model.UserName;

        if (!firstNameChanged && !lastNameChanged && !emailChanged && !usernameChanged)
        {
            return ProfileUpdateResult.NoChanges();
        }

        if (emailChanged)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ProfileUpdateResult.Failure(ProfileUpdateStatus.EmailFailure);
            }
        }

        if (usernameChanged)
        {
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ProfileUpdateResult.Failure(ProfileUpdateStatus.UsernameFailure);
            }
        }

        user.Ime = model.Ime;
        user.Prezime = model.Prezime;
        user.Email = model.Email;
        user.UserName = model.UserName;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var status = emailChanged
                ? ProfileUpdateStatus.EmailFailure
                : usernameChanged
                    ? ProfileUpdateStatus.UsernameFailure
                    : ProfileUpdateStatus.IdentityFailure;
            return ProfileUpdateResult.Failure(status, updateResult.Errors);
        }

        await _signInManager.RefreshSignInAsync(user);
        _audit.Success(
            "ProfileUpdated",
            "User",
            user.Id,
            new
            {
                Before = previous,
                After = new { user.Ime, user.Prezime, user.Email, user.UserName }
            });
        return ProfileUpdateResult.Updated();
    }

    public async Task<IdentityOperationResult> ChangePasswordAsync(
        ApplicationUser user,
        string currentPassword,
        string newPassword)
    {
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(result.Errors);
        }

        await _signInManager.RefreshSignInAsync(user);
        _audit.Success(
            "PasswordChanged",
            "User",
            user.Id,
            new { user.UserName, user.Email });
        return IdentityOperationResult.Success();
    }
}

public enum ProfileUpdateStatus
{
    Updated,
    NoChanges,
    EmailFailure,
    UsernameFailure,
    IdentityFailure
}

public sealed record ProfileUpdateResult(ProfileUpdateStatus Status, IReadOnlyList<string> Errors)
{
    public static ProfileUpdateResult Updated() => new(ProfileUpdateStatus.Updated, Array.Empty<string>());
    public static ProfileUpdateResult NoChanges() => new(ProfileUpdateStatus.NoChanges, Array.Empty<string>());
    public static ProfileUpdateResult Failure(
        ProfileUpdateStatus status,
        IEnumerable<IdentityError>? errors = null) =>
        new(
            status,
            errors == null
                ? Array.Empty<string>()
                : errors.Select(error => error.Description).ToList());
}

public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static IdentityOperationResult Success() => new(true, Array.Empty<string>());
    public static IdentityOperationResult Failure(IEnumerable<IdentityError> errors) =>
        new(false, errors.Select(error => error.Description).ToList());
}
