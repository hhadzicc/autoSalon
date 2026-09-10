using Autosalon_OneZone.Authorization;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IAdminProfileService
{
    Task<AddProfilViewModel> GetCreateModelAsync();
    Task<AddProfilViewModel?> GetEditModelAsync(string id);
    Task<RoleValidationResult> ValidateRolesAsync(IEnumerable<string>? requestedRoles);
    Task<ProfileSaveResult> SaveAsync(AddProfilViewModel model, string? currentUserId);
    Task<ProfileDeleteResult> DeleteAsync(string id, string? currentUserId);
}

public sealed class AdminProfileService : IAdminProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminProfileService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<AddProfilViewModel> GetCreateModelAsync() => new()
    {
        DostupneRole = await _roleManager.Roles.AsNoTracking().ToListAsync()
    };

    public async Task<AddProfilViewModel?> GetEditModelAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        return new AddProfilViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Ime = user.Ime,
            Prezime = user.Prezime,
            DostupneRole = await _roleManager.Roles.AsNoTracking().ToListAsync(),
            OdabraneRole = (await _userManager.GetRolesAsync(user)).ToList()
        };
    }

    public async Task<RoleValidationResult> ValidateRolesAsync(IEnumerable<string>? requestedRoles)
    {
        var normalizedRoles = NormalizeRoles(requestedRoles);
        if (normalizedRoles.Count == 0)
        {
            return new RoleValidationResult(new List<string>(), new List<string>());
        }

        var availableRoles = await _roleManager.Roles
            .Select(role => role.Name)
            .Where(roleName => roleName != null)
            .ToListAsync();
        var roleLookup = availableRoles.ToDictionary(
            roleName => roleName!,
            roleName => roleName!,
            StringComparer.OrdinalIgnoreCase);
        var validRoles = new List<string>();
        var invalidRoles = new List<string>();

        foreach (var role in normalizedRoles)
        {
            if (roleLookup.TryGetValue(role, out var validRole))
            {
                validRoles.Add(validRole);
            }
            else
            {
                invalidRoles.Add(role);
            }
        }

        return new RoleValidationResult(validRoles, invalidRoles);
    }

    public async Task<ProfileSaveResult> SaveAsync(AddProfilViewModel model, string? currentUserId)
    {
        var isNew = string.IsNullOrEmpty(model.UserId);
        ApplicationUser user;

        if (isNew)
        {
            user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                Ime = model.Ime ?? string.Empty,
                Prezime = model.Prezime ?? string.Empty
            };
        }
        else
        {
            user = await _userManager.FindByIdAsync(model.UserId!);
            if (user == null)
            {
                return ProfileSaveResult.Failure(ProfileSaveStatus.NotFound);
            }

            if (user.UserName != model.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return ProfileSaveResult.Failure(ProfileSaveStatus.UsernameExists);
                }
            }

            if (user.Email != model.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return ProfileSaveResult.Failure(ProfileSaveStatus.EmailExists);
                }
            }
        }

        var currentRoles = isNew
            ? Array.Empty<string>()
            : await _userManager.GetRolesAsync(user);
        var requestedRoles = model.OdabraneRole ?? new List<string>();
        var removesAdministrator = HasRole(currentRoles, AppRoles.Administrator) &&
                                   !HasRole(requestedRoles, AppRoles.Administrator);

        if (removesAdministrator)
        {
            if (user.Id == currentUserId)
            {
                return ProfileSaveResult.Failure(ProfileSaveStatus.CannotRemoveOwnAdministratorRole);
            }

            var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Administrator);
            if (administrators.Count == 1 && administrators[0].Id == user.Id)
            {
                return ProfileSaveResult.Failure(ProfileSaveStatus.CannotRemoveOnlyAdministratorRole);
            }
        }

        if (isNew)
        {
            var createResult = await _userManager.CreateAsync(user, model.Password!);
            if (!createResult.Succeeded)
            {
                return ProfileSaveResult.IdentityFailure(createResult.Errors);
            }
        }
        else
        {
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.Ime = model.Ime ?? string.Empty;
            user.Prezime = model.Prezime ?? string.Empty;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ProfileSaveResult.IdentityFailure(updateResult.Errors);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    return ProfileSaveResult.IdentityFailure(passwordResult.Errors);
                }
            }
        }

        var roleResult = await UpdateRolesAsync(user, currentRoles, requestedRoles);
        if (!roleResult.Succeeded)
        {
            if (isNew)
            {
                await _userManager.DeleteAsync(user);
            }

            return ProfileSaveResult.IdentityFailure(roleResult.Errors);
        }

        return ProfileSaveResult.Saved(user.Id);
    }

    public async Task<ProfileDeleteResult> DeleteAsync(string id, string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return ProfileDeleteResult.Failure(ProfileDeleteStatus.NotFound);
        }

        if (user.Id == currentUserId && await _userManager.IsInRoleAsync(user, AppRoles.Administrator))
        {
            return ProfileDeleteResult.Failure(ProfileDeleteStatus.CannotDeleteOwnAdministratorAccount);
        }

        var orderIds = await _context.Narudzbe
            .Where(order => order.KorisnikId == id)
            .Select(order => order.NarudzbaID)
            .ToListAsync();

        if (orderIds.Count > 0)
        {
            var payments = await _context.Placanja
                .Where(payment => orderIds.Contains(payment.NarudzbaID))
                .ToListAsync();
            var orderItems = await _context.StavkeKorpe
                .Where(item => item.NarudzbaID.HasValue && orderIds.Contains(item.NarudzbaID.Value))
                .ToListAsync();

            _context.Placanja.RemoveRange(payments);
            _context.StavkeKorpe.RemoveRange(orderItems);
            await _context.SaveChangesAsync();
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        return deleteResult.Succeeded
            ? ProfileDeleteResult.Deleted()
            : ProfileDeleteResult.IdentityFailure(deleteResult.Errors);
    }

    private async Task<IdentityResult> UpdateRolesAsync(
        ApplicationUser user,
        IEnumerable<string> currentRoles,
        IReadOnlyCollection<string> requestedRoles)
    {
        if (requestedRoles.Count == 0)
        {
            return currentRoles.Any()
                ? IdentityResult.Success
                : await _userManager.AddToRoleAsync(user, AppRoles.Buyer);
        }

        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        return rolesToRemove.Count > 0
            ? await _userManager.RemoveFromRolesAsync(user, rolesToRemove)
            : IdentityResult.Success;
    }

    private static List<string> NormalizeRoles(IEnumerable<string>? roles) =>
        roles == null
            ? new List<string>()
            : roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .SelectMany(role => role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    private static bool HasRole(IEnumerable<string> roles, string role) =>
        roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

public sealed record RoleValidationResult(List<string> ValidRoles, List<string> InvalidRoles);

public enum ProfileSaveStatus
{
    Saved,
    NotFound,
    UsernameExists,
    EmailExists,
    CannotRemoveOwnAdministratorRole,
    CannotRemoveOnlyAdministratorRole,
    IdentityFailure
}

public sealed record ProfileSaveResult(
    ProfileSaveStatus Status,
    string? UserId = null,
    IReadOnlyList<string>? Errors = null)
{
    public static ProfileSaveResult Saved(string userId) => new(ProfileSaveStatus.Saved, userId);
    public static ProfileSaveResult Failure(ProfileSaveStatus status) => new(status);
    public static ProfileSaveResult IdentityFailure(IEnumerable<IdentityError> errors) =>
        new(ProfileSaveStatus.IdentityFailure, Errors: errors.Select(error => error.Description).ToList());
}

public enum ProfileDeleteStatus
{
    Deleted,
    NotFound,
    CannotDeleteOwnAdministratorAccount,
    IdentityFailure
}

public sealed record ProfileDeleteResult(
    ProfileDeleteStatus Status,
    IReadOnlyList<string>? Errors = null)
{
    public static ProfileDeleteResult Deleted() => new(ProfileDeleteStatus.Deleted);
    public static ProfileDeleteResult Failure(ProfileDeleteStatus status) => new(status);
    public static ProfileDeleteResult IdentityFailure(IEnumerable<IdentityError> errors) =>
        new(ProfileDeleteStatus.IdentityFailure, errors.Select(error => error.Description).ToList());
}
