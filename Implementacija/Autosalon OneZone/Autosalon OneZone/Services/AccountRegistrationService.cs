using Autosalon_OneZone.Authorization;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Services;

public interface IAccountRegistrationService
{
    Task<AccountRegistrationResult> RegisterAsync(RegisterViewModel model);
}

public sealed class AccountRegistrationService : IAccountRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountRegistrationService> _logger;

    public AccountRegistrationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountRegistrationService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<AccountRegistrationResult> RegisterAsync(RegisterViewModel model)
    {
        if (await _userManager.FindByNameAsync(model.UserName) != null)
        {
            return AccountRegistrationResult.UsernameTaken();
        }

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            return AccountRegistrationResult.EmailTaken();
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            Ime = model.Ime,
            Prezime = model.Prezime
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return AccountRegistrationResult.IdentityFailure(createResult.Errors);
        }

        _logger.LogInformation("User created a new account with password.");

        if (!await _roleManager.RoleExistsAsync(AppRoles.Buyer))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(AppRoles.Buyer));
            if (!roleResult.Succeeded)
            {
                await DeleteIncompleteUserAsync(user);
                return AccountRegistrationResult.IdentityFailure(roleResult.Errors);
            }

            _logger.LogInformation("Role '{Role}' created.", AppRoles.Buyer);
        }

        var roleAssignmentResult = await _userManager.AddToRoleAsync(user, AppRoles.Buyer);
        if (!roleAssignmentResult.Succeeded)
        {
            await DeleteIncompleteUserAsync(user);
            return AccountRegistrationResult.IdentityFailure(roleAssignmentResult.Errors);
        }

        _logger.LogInformation("User '{UserName}' added to role '{Role}'.", user.UserName, AppRoles.Buyer);

        return AccountRegistrationResult.Success();
    }

    private async Task DeleteIncompleteUserAsync(ApplicationUser user)
    {
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            _logger.LogError(
                "Failed to remove incomplete account {UserId}: {Errors}",
                user.Id,
                string.Join("; ", deleteResult.Errors.Select(error => error.Description)));
        }
    }
}

public enum AccountRegistrationStatus
{
    Succeeded,
    UsernameTaken,
    EmailTaken,
    IdentityFailure
}

public sealed record AccountRegistrationResult(
    AccountRegistrationStatus Status,
    IReadOnlyList<string> Errors)
{
    public static AccountRegistrationResult Success() =>
        new(AccountRegistrationStatus.Succeeded, Array.Empty<string>());

    public static AccountRegistrationResult UsernameTaken() =>
        new(AccountRegistrationStatus.UsernameTaken, Array.Empty<string>());

    public static AccountRegistrationResult EmailTaken() =>
        new(AccountRegistrationStatus.EmailTaken, Array.Empty<string>());

    public static AccountRegistrationResult IdentityFailure(IEnumerable<IdentityError> errors) =>
        new(
            AccountRegistrationStatus.IdentityFailure,
            errors.Select(error => error.Description).ToList());
}
