using System.Text.Json.Serialization;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IAdminListQueryService
{
    Task<VoziloListViewModel> GetVehicleSectionAsync(string? searchQuery);
    Task<ProfilListViewModel> GetProfileSectionAsync(string? searchQuery);
    Task<AdminPageResult<SupportListItem>> GetSupportAsync(string? searchQuery);
    Task<AdminPageResult<ReviewListItem>> GetReviewsAsync(
        string? searchQuery,
        string? userFilter,
        string? vehicleFilter);
    Task<VehiclePageResult> GetVehiclesAsync(
        string? searchQuery,
        string? sortOrder,
        string? fuelFilter,
        string? sort,
        string? direction);
    Task<AdminPageResult<ProfileListItem>> GetProfilesAsync(string? searchQuery, string? roleFilter);
}

public sealed class AdminListQueryService : IAdminListQueryService
{
    private const int AllItemsPageSize = int.MaxValue;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminListQueryService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<VoziloListViewModel> GetVehicleSectionAsync(string? searchQuery)
    {
        var query = FilterVehicles(_context.Vozila.AsNoTracking(), searchQuery);
        return new VoziloListViewModel
        {
            Vozila = await query.OrderByDescending(vehicle => vehicle.VoziloID).ToListAsync(),
            SearchQuery = searchQuery
        };
    }

    public async Task<ProfilListViewModel> GetProfileSectionAsync(string? searchQuery)
    {
        var query = FilterProfiles(_context.Users.AsNoTracking(), searchQuery);
        return new ProfilListViewModel
        {
            Profili = await query.ToListAsync(),
            SearchQuery = searchQuery
        };
    }

    public async Task<AdminPageResult<SupportListItem>> GetSupportAsync(string? searchQuery)
    {
        var query = _context.PodrskaUpiti
            .AsNoTracking()
            .Include(request => request.Korisnik)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(request =>
                (request.Naslov != null && request.Naslov.Contains(searchQuery)) ||
                (request.Sadrzaj != null && request.Sadrzaj.Contains(searchQuery)) ||
                (request.Korisnik != null && request.Korisnik.Email!.Contains(searchQuery)));
        }

        var totalCount = await query.CountAsync();
        var requests = await query
            .OrderByDescending(request => request.DatumUpita)
            .ToListAsync();
        var items = requests.Select(request => new SupportListItem(
                request.UpitID,
                request.DatumUpita,
                request.KorisnikId,
                request.Korisnik != null ? request.Korisnik.Email ?? "N/A" : "N/A",
                request.Korisnik != null
                    ? ((request.Korisnik.Ime ?? "") + " " + (request.Korisnik.Prezime ?? "")).Trim()
                    : "",
                request.Naslov,
                request.Sadrzaj,
                request.Status.ToString()))
            .ToList();

        return Page(items, totalCount);
    }

    public async Task<AdminPageResult<ReviewListItem>> GetReviewsAsync(
        string? searchQuery,
        string? userFilter,
        string? vehicleFilter)
    {
        var query = _context.Recenzije
            .AsNoTracking()
            .Include(review => review.Korisnik)
            .Include(review => review.Vozilo)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userFilter))
        {
            query = query.Where(review =>
                (review.Korisnik.UserName != null && review.Korisnik.UserName.Contains(userFilter)) ||
                (review.Korisnik.Email != null && review.Korisnik.Email.Contains(userFilter)) ||
                (review.Korisnik.Ime != null && review.Korisnik.Ime.Contains(userFilter)) ||
                (review.Korisnik.Prezime != null && review.Korisnik.Prezime.Contains(userFilter)));
        }

        if (!string.IsNullOrEmpty(vehicleFilter))
        {
            query = query.Where(review =>
                (review.Vozilo.Marka != null && review.Vozilo.Marka.Contains(vehicleFilter)) ||
                (review.Vozilo.Model != null && review.Vozilo.Model.Contains(vehicleFilter)));
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(review => review.Komentar != null && review.Komentar.Contains(searchQuery));
        }

        var totalCount = await query.CountAsync();
        var reviews = await query.OrderByDescending(review => review.DatumRecenzije).ToListAsync();
        var items = reviews.Select(review => new ReviewListItem(
            review.RecenzijaID,
            review.KorisnikId,
            review.Korisnik?.UserName ?? "N/A",
            $"{review.Korisnik?.Ime ?? ""} {review.Korisnik?.Prezime ?? ""}".Trim(),
            review.VoziloID,
            review.Vozilo?.Marka ?? "N/A",
            review.Vozilo?.Model ?? "",
            $"{review.Vozilo?.Marka ?? ""} {review.Vozilo?.Model ?? ""}".Trim(),
            review.Ocjena,
            review.Komentar,
            review.DatumRecenzije)).ToList();

        return Page(items, totalCount);
    }

    public async Task<VehiclePageResult> GetVehiclesAsync(
        string? searchQuery,
        string? sortOrder,
        string? fuelFilter,
        string? sort,
        string? direction)
    {
        var query = FilterVehicles(_context.Vozila.AsNoTracking(), searchQuery);

        if (!string.IsNullOrWhiteSpace(fuelFilter) &&
            Enum.TryParse<TipGoriva>(fuelFilter, true, out var fuel) &&
            Enum.IsDefined(typeof(TipGoriva), fuel))
        {
            query = query.Where(vehicle => vehicle.Gorivo == fuel);
        }

        (sort, direction) = ResolveVehicleSort(sortOrder, sort, direction);
        query = sort switch
        {
            "godiste" => direction == "asc"
                ? query.OrderBy(vehicle => vehicle.Godiste)
                : query.OrderByDescending(vehicle => vehicle.Godiste),
            "kilometraza" => direction == "desc"
                ? query.OrderByDescending(vehicle => vehicle.Kilometraza)
                : query.OrderBy(vehicle => vehicle.Kilometraza),
            "cijena" => direction == "asc"
                ? query.OrderBy(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0)
                : query.OrderByDescending(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0),
            _ => query.OrderByDescending(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0)
        };

        var totalCount = await query.CountAsync();
        var vehicles = await query.ToListAsync();
        var items = vehicles.Select(vehicle => new VehicleListItem(
            vehicle.VoziloID,
            $"{vehicle.Marka} {vehicle.Model}".Trim(),
            vehicle.Godiste,
            vehicle.Gorivo.ToString(),
            vehicle.Kilometraza,
            vehicle.Cijena,
            vehicle.Boja,
            vehicle.Kubikaza)).ToList();

        return new VehiclePageResult(items, totalCount, PageCount(totalCount), 1, AllItemsPageSize, sort, direction);
    }

    public async Task<AdminPageResult<ProfileListItem>> GetProfilesAsync(string? searchQuery, string? roleFilter)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(roleFilter) &&
            !string.Equals(roleFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
            var userIds = usersInRole.Select(user => user.Id).ToList();
            query = query.Where(user => userIds.Contains(user.Id));
        }

        query = FilterProfiles(query, searchQuery).OrderBy(user => user.UserName);
        var totalCount = await query.CountAsync();
        var profiles = await query.ToListAsync();
        var items = new List<ProfileListItem>(profiles.Count);

        foreach (var user in profiles)
        {
            items.Add(new ProfileListItem(
                user.Id,
                user.UserName,
                user.Email,
                user.Ime,
                user.Prezime,
                string.Join(", ", await _userManager.GetRolesAsync(user))));
        }

        return Page(items, totalCount);
    }

    private static IQueryable<Vozilo> FilterVehicles(IQueryable<Vozilo> query, string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return query;
        }

        return query.Where(vehicle =>
            (vehicle.Marka != null && vehicle.Marka.Contains(searchQuery)) ||
            (vehicle.Model != null && vehicle.Model.Contains(searchQuery)));
    }

    private static IQueryable<ApplicationUser> FilterProfiles(
        IQueryable<ApplicationUser> query,
        string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return query;
        }

        return query.Where(user =>
            (user.UserName != null && user.UserName.Contains(searchQuery)) ||
            (user.Email != null && user.Email.Contains(searchQuery)) ||
            (user.Ime != null && user.Ime.Contains(searchQuery)) ||
            (user.Prezime != null && user.Prezime.Contains(searchQuery)));
    }

    private static (string Sort, string Direction) ResolveVehicleSort(
        string? sortOrder,
        string? sort,
        string? direction)
    {
        sort = string.IsNullOrWhiteSpace(sort) ? null : sort.ToLowerInvariant();
        direction = string.IsNullOrWhiteSpace(direction) ? null : direction.ToLowerInvariant();

        if (sort == null)
        {
            (sort, direction) = sortOrder switch
            {
                "price_asc" => ("cijena", "asc"),
                "price_desc" => ("cijena", "desc"),
                "year_asc" => ("godiste", "asc"),
                "year_desc" => ("godiste", "desc"),
                _ => (null, direction)
            };
        }

        sort ??= "cijena";
        if (direction is not "asc" and not "desc")
        {
            direction = sort == "kilometraza" ? "asc" : "desc";
        }

        return (sort, direction);
    }

    private static AdminPageResult<T> Page<T>(List<T> items, int totalCount) =>
        new(items, totalCount, PageCount(totalCount), 1, AllItemsPageSize);

    private static int PageCount(int totalCount) =>
        (int)Math.Ceiling(totalCount / (double)AllItemsPageSize);
}

public sealed record AdminPageResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int TotalPages,
    int CurrentPage,
    int PageSize);

public sealed record VehiclePageResult(
    IReadOnlyList<VehicleListItem> Items,
    int TotalCount,
    int TotalPages,
    int CurrentPage,
    int PageSize,
    string Sort,
    string Direction);

public sealed record SupportListItem(
    [property: JsonPropertyName("upitID")] int UpitId,
    [property: JsonPropertyName("datumUpita")] DateTime Date,
    [property: JsonPropertyName("korisnikId")] string UserId,
    [property: JsonPropertyName("korisnikEmail")] string UserEmail,
    [property: JsonPropertyName("korisnikIme")] string UserName,
    [property: JsonPropertyName("naslov")] string Title,
    [property: JsonPropertyName("sadrzaj")] string Content,
    [property: JsonPropertyName("status")] string Status);

public sealed record ReviewListItem(
    [property: JsonPropertyName("recenzijaID")] int ReviewId,
    [property: JsonPropertyName("korisnikId")] string UserId,
    [property: JsonPropertyName("korisnikUserName")] string UserAccountName,
    [property: JsonPropertyName("korisnikIme")] string UserName,
    [property: JsonPropertyName("voziloID")] int VehicleId,
    [property: JsonPropertyName("voziloMarka")] string VehicleMake,
    [property: JsonPropertyName("voziloModel")] string VehicleModel,
    [property: JsonPropertyName("voziloNaziv")] string VehicleName,
    [property: JsonPropertyName("ocjena")] int Rating,
    [property: JsonPropertyName("komentar")] string Comment,
    [property: JsonPropertyName("datumRecenzije")] DateTime Date);

public sealed record VehicleListItem(
    [property: JsonPropertyName("voziloID")] int VehicleId,
    [property: JsonPropertyName("naziv")] string Name,
    [property: JsonPropertyName("godiste")] int? Year,
    [property: JsonPropertyName("gorivo")] string Fuel,
    [property: JsonPropertyName("kilometraza")] double? Mileage,
    [property: JsonPropertyName("cijena")] decimal? Price,
    [property: JsonPropertyName("boja")] string Color,
    [property: JsonPropertyName("kubikaza")] decimal? EngineDisplacement);

public sealed record ProfileListItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("userName")] string? UserName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("ime")] string FirstName,
    [property: JsonPropertyName("prezime")] string LastName,
    [property: JsonPropertyName("uloga")] string Role);
