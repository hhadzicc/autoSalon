using System.Text.Json.Serialization;
using Autosalon_OneZone.Data;
using Autosalon_OneZone.Models;
using Autosalon_OneZone.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Autosalon_OneZone.Services;

public interface IAdminListQueryService
{
    Task<VoziloListViewModel> GetVehicleSectionAsync(string? searchQuery);
    Task<ProfilListViewModel> GetProfileSectionAsync(string? searchQuery);
    Task<RecenzijaListViewModel> GetReviewSectionAsync(string? searchQuery);
    Task<IReadOnlyList<AdminFilterOption>> GetReviewCustomerSuggestionsAsync(string? query, string? selectedId);
    Task<IReadOnlyList<AdminFilterOption>> GetReviewVehicleSuggestionsAsync(string? query, int? selectedId);
    Task<AdminPageResult<SupportListItem>> GetSupportAsync(string? searchQuery, int page, int? offset);
    Task<AdminPageResult<ReviewListItem>> GetReviewsAsync(
        string? searchQuery,
        string? userIdFilter,
        string? userFilter,
        int? vehicleIdFilter,
        string? vehicleFilter,
        int? ratingFilter,
        string? sort,
        string? direction,
        int page,
        int? offset);
    Task<VehiclePageResult> GetVehiclesAsync(
        string? searchQuery,
        string? sortOrder,
        string? fuelFilter,
        string? colorFilter,
        string? statusFilter,
        string? sort,
        string? direction,
        int page,
        int? offset);
    Task<AdminPageResult<ProfileListItem>> GetProfilesAsync(string? searchQuery, string? roleFilter, string? userIdFilter, int page, int? offset);
}

public sealed class AdminListQueryService : IAdminListQueryService
{
    public const int AdminPageSize = 25;
    private readonly ApplicationDbContext _context;

    public AdminListQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<VoziloListViewModel> GetVehicleSectionAsync(string? searchQuery)
    {
        return Task.FromResult(new VoziloListViewModel
        {
            SearchQuery = searchQuery
        });
    }

    public Task<ProfilListViewModel> GetProfileSectionAsync(string? searchQuery)
    {
        return Task.FromResult(new ProfilListViewModel
        {
            SearchQuery = searchQuery
        });
    }

    public Task<RecenzijaListViewModel> GetReviewSectionAsync(string? searchQuery)
    {
        return Task.FromResult(new RecenzijaListViewModel
        {
            SearchQuery = searchQuery
        });
    }

    public async Task<IReadOnlyList<AdminFilterOption>> GetReviewCustomerSuggestionsAsync(
        string? query,
        string? selectedId)
    {
        var reviews = _context.Recenzije
            .AsNoTracking()
            .Where(review => review.Korisnik != null);

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            reviews = reviews.Where(review => review.KorisnikId == selectedId);
        }
        else if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            reviews = reviews.Where(review =>
                (review.Korisnik!.UserName != null && review.Korisnik.UserName.Contains(term)) ||
                (review.Korisnik.Email != null && review.Korisnik.Email.Contains(term)) ||
                (review.Korisnik.Ime != null && review.Korisnik.Ime.Contains(term)) ||
                (review.Korisnik.Prezime != null && review.Korisnik.Prezime.Contains(term)));
        }

        var customers = await reviews
            .Select(review => new
            {
                review.KorisnikId,
                review.Korisnik!.UserName,
                review.Korisnik.Email,
                review.Korisnik.Ime,
                review.Korisnik.Prezime
            })
            .Distinct()
            .OrderBy(customer => customer.Ime)
            .ThenBy(customer => customer.Prezime)
            .ThenBy(customer => customer.UserName)
            .Take(20)
            .ToListAsync();

        return customers.Select(customer =>
        {
            var fullName = $"{customer.Ime ?? ""} {customer.Prezime ?? ""}".Trim();
            var accountName = customer.UserName ?? customer.Email ?? customer.KorisnikId;
            var label = string.IsNullOrWhiteSpace(fullName) ? accountName : $"{fullName} ({accountName})";
            return new AdminFilterOption(customer.KorisnikId, label);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminFilterOption>> GetReviewVehicleSuggestionsAsync(
        string? query,
        int? selectedId)
    {
        var reviews = _context.Recenzije
            .AsNoTracking()
            .Where(review => review.Vozilo != null);

        if (selectedId is > 0)
        {
            reviews = reviews.Where(review => review.VoziloID == selectedId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            reviews = reviews.Where(review =>
                (review.Vozilo!.Marka != null && review.Vozilo.Marka.Contains(term)) ||
                (review.Vozilo.Model != null && review.Vozilo.Model.Contains(term)));
        }

        var vehicles = await reviews
            .Select(review => new
            {
                review.VoziloID,
                review.Vozilo!.Marka,
                review.Vozilo.Model
            })
            .Distinct()
            .OrderBy(vehicle => vehicle.Marka)
            .ThenBy(vehicle => vehicle.Model)
            .Take(20)
            .ToListAsync();

        return vehicles
            .Select(vehicle => new AdminFilterOption(
                vehicle.VoziloID.ToString(),
                $"{vehicle.Marka ?? ""} {vehicle.Model ?? ""}".Trim()))
            .ToList();
    }

    public async Task<AdminPageResult<SupportListItem>> GetSupportAsync(string? searchQuery, int page, int? offset)
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

        query = query
            .OrderByDescending(request => request.DatumUpita)
            .ThenByDescending(request => request.UpitID);
        var pageResult = await PaginateAsync(query, page, offset);
        var items = pageResult.Items.Select(request => new SupportListItem(
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

        return ToPageResult(items, pageResult);
    }

    public async Task<AdminPageResult<ReviewListItem>> GetReviewsAsync(
        string? searchQuery,
        string? userIdFilter,
        string? userFilter,
        int? vehicleIdFilter,
        string? vehicleFilter,
        int? ratingFilter,
        string? sort,
        string? direction,
        int page,
        int? offset)
    {
        var query = _context.Recenzije
            .AsNoTracking()
            .Include(review => review.Korisnik)
            .Include(review => review.Vozilo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userIdFilter))
        {
            query = query.Where(review => review.KorisnikId == userIdFilter);
        }
        else if (!string.IsNullOrWhiteSpace(userFilter))
        {
            query = query.Where(review =>
                (review.Korisnik.UserName != null && review.Korisnik.UserName.Contains(userFilter)) ||
                (review.Korisnik.Email != null && review.Korisnik.Email.Contains(userFilter)) ||
                (review.Korisnik.Ime != null && review.Korisnik.Ime.Contains(userFilter)) ||
                (review.Korisnik.Prezime != null && review.Korisnik.Prezime.Contains(userFilter)));
        }

        if (vehicleIdFilter is > 0)
        {
            query = query.Where(review => review.VoziloID == vehicleIdFilter.Value);
        }
        else if (!string.IsNullOrWhiteSpace(vehicleFilter))
        {
            query = query.Where(review =>
                (review.Vozilo.Marka != null && review.Vozilo.Marka.Contains(vehicleFilter)) ||
                (review.Vozilo.Model != null && review.Vozilo.Model.Contains(vehicleFilter)));
        }

        if (ratingFilter is >= 1 and <= 5)
        {
            query = query.Where(review => review.Ocjena == ratingFilter.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(review => review.Komentar != null && review.Komentar.Contains(searchQuery));
        }

        sort = string.Equals(sort, "ocjena", StringComparison.OrdinalIgnoreCase) ? "ocjena" : "datum";
        direction = string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        query = sort switch
        {
            "ocjena" when direction == "asc" => query.OrderBy(review => review.Ocjena).ThenByDescending(review => review.DatumRecenzije).ThenByDescending(review => review.RecenzijaID),
            "ocjena" => query.OrderByDescending(review => review.Ocjena).ThenByDescending(review => review.DatumRecenzije).ThenByDescending(review => review.RecenzijaID),
            "datum" when direction == "asc" => query.OrderBy(review => review.DatumRecenzije).ThenBy(review => review.RecenzijaID),
            _ => query.OrderByDescending(review => review.DatumRecenzije).ThenByDescending(review => review.RecenzijaID)
        };

        var pageResult = await PaginateAsync(query, page, offset);
        var items = pageResult.Items.Select(review => new ReviewListItem(
            review.RecenzijaID,
            review.KorisnikId,
            review.Korisnik?.UserName ?? "N/A",
            $"{review.Korisnik?.Ime ?? ""} {review.Korisnik?.Prezime ?? ""}".Trim(),
            review.Korisnik?.Email ?? "",
            review.VoziloID,
            review.Vozilo?.Marka ?? "N/A",
            review.Vozilo?.Model ?? "",
            $"{review.Vozilo?.Marka ?? ""} {review.Vozilo?.Model ?? ""}".Trim(),
            review.Ocjena,
            review.Komentar,
            review.DatumRecenzije)).ToList();

        return ToPageResult(items, pageResult);
    }

    public async Task<VehiclePageResult> GetVehiclesAsync(
        string? searchQuery,
        string? sortOrder,
        string? fuelFilter,
        string? colorFilter,
        string? statusFilter,
        string? sort,
        string? direction,
        int page,
        int? offset)
    {
        var query = FilterVehicles(_context.Vozila.AsNoTracking(), searchQuery);

        if (!string.IsNullOrWhiteSpace(fuelFilter) &&
            Enum.TryParse<TipGoriva>(fuelFilter, true, out var fuel) &&
            Enum.IsDefined(typeof(TipGoriva), fuel))
        {
            query = query.Where(vehicle => vehicle.Gorivo == fuel);
        }

        if (!string.IsNullOrWhiteSpace(colorFilter) &&
            Enum.TryParse<TipBoje>(colorFilter, true, out var color) &&
            Enum.IsDefined(typeof(TipBoje), color))
        {
            query = query.Where(vehicle => vehicle.Boja == color);
        }

        var normalizedStatus = statusFilter?.Trim().ToLowerInvariant();
        normalizedStatus = normalizedStatus is "all" or "sold" ? normalizedStatus : "available";
        query = normalizedStatus switch
        {
            "all" => query,
            "sold" => query.UnavailableForPurchase(),
            _ => query.AvailableForPurchase()
        };

        (sort, direction) = ResolveVehicleSort(sortOrder, sort, direction);
        query = sort switch
        {
            "godiste" => direction == "asc"
                ? query.OrderBy(vehicle => vehicle.Godiste).ThenBy(vehicle => vehicle.VoziloID)
                : query.OrderByDescending(vehicle => vehicle.Godiste).ThenByDescending(vehicle => vehicle.VoziloID),
            "kilometraza" => direction == "desc"
                ? query.OrderByDescending(vehicle => vehicle.Kilometraza).ThenByDescending(vehicle => vehicle.VoziloID)
                : query.OrderBy(vehicle => vehicle.Kilometraza).ThenBy(vehicle => vehicle.VoziloID),
            "cijena" => direction == "asc"
                ? query.OrderBy(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0).ThenBy(vehicle => vehicle.VoziloID)
                : query.OrderByDescending(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0).ThenByDescending(vehicle => vehicle.VoziloID),
            _ => query.OrderByDescending(vehicle => vehicle.Cijena.HasValue ? (double)vehicle.Cijena.Value : 0).ThenByDescending(vehicle => vehicle.VoziloID)
        };

        var pageResult = await PaginateAsync(query, page, offset);
        var pageVehicleIds = pageResult.Items.Select(vehicle => vehicle.VoziloID).ToArray();
        HashSet<int> unavailableVehicleIds;
        if (normalizedStatus == "sold")
        {
            unavailableVehicleIds = pageVehicleIds.ToHashSet();
        }
        else if (normalizedStatus == "all" && pageVehicleIds.Length > 0)
        {
            unavailableVehicleIds = (await _context.Vozila
                .AsNoTracking()
                .UnavailableForPurchase()
                .Where(vehicle => pageVehicleIds.Contains(vehicle.VoziloID))
                .Select(vehicle => vehicle.VoziloID)
                .ToListAsync()).ToHashSet();
        }
        else
        {
            unavailableVehicleIds = [];
        }

        var items = pageResult.Items.Select(vehicle => new VehicleListItem(
            vehicle.VoziloID,
            $"{vehicle.Marka} {vehicle.Model}".Trim(),
            vehicle.Godiste,
            vehicle.Gorivo.ToString(),
            vehicle.Kilometraza,
            vehicle.Cijena,
            vehicle.Boja.ToString(),
            vehicle.Kubikaza,
            !unavailableVehicleIds.Contains(vehicle.VoziloID))).ToList();

        return new VehiclePageResult(
            items,
            pageResult.TotalCount,
            pageResult.TotalPages,
            pageResult.CurrentPage,
            AdminPageSize,
            pageResult.Offset,
            sort,
            direction);
    }

    public async Task<AdminPageResult<ProfileListItem>> GetProfilesAsync(
        string? searchQuery,
        string? roleFilter,
        string? userIdFilter,
        int page,
        int? offset)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(userIdFilter))
        {
            query = query.Where(user => user.Id == userIdFilter);
        }

        if (!string.IsNullOrWhiteSpace(roleFilter) &&
            !string.Equals(roleFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedRole = roleFilter.Trim().ToUpperInvariant();
            query = query.Where(user => _context.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                _context.Roles.Any(role =>
                    role.Id == userRole.RoleId && role.NormalizedName == normalizedRole)));
        }

        query = FilterProfiles(query, searchQuery)
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id);
        var pageResult = await PaginateAsync(query, page, offset);
        var userIds = pageResult.Items.Select(user => user.Id).ToList();
        var userRoles = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync();
        var rolesByUser = userRoles.ToLookup(item => item.UserId, item => item.Name);
        var items = pageResult.Items
            .Select(user => new ProfileListItem(
                user.Id,
                user.UserName,
                user.Email,
                user.Ime,
                user.Prezime,
                string.Join(", ", rolesByUser[user.Id].Where(role => role != null))))
            .ToList();

        return ToPageResult(items, pageResult);
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

    private static async Task<QueryPage<T>> PaginateAsync<T>(IQueryable<T> query, int requestedPage, int? requestedOffset)
    {
        var totalCount = await query.CountAsync();
        var totalPages = PageCount(totalCount);
        var currentPage = totalPages == 0
            ? 1
            : Math.Clamp(requestedPage, 1, totalPages);
        var offset = requestedOffset.HasValue
            ? Math.Clamp(requestedOffset.Value, 0, totalCount)
            : (currentPage - 1) * AdminPageSize;
        if (requestedOffset.HasValue && totalPages > 0)
        {
            currentPage = Math.Min((offset / AdminPageSize) + 1, totalPages);
        }
        List<T> items = totalCount == 0
            ? []
            : await query
                .Skip(offset)
                .Take(AdminPageSize)
                .ToListAsync();

        return new QueryPage<T>(items, totalCount, totalPages, currentPage, offset);
    }

    private static AdminPageResult<TResult> ToPageResult<TSource, TResult>(
        IReadOnlyList<TResult> items,
        QueryPage<TSource> page) =>
        new(items, page.TotalCount, page.TotalPages, page.CurrentPage, AdminPageSize, page.Offset);

    private static int PageCount(int totalCount) =>
        (int)Math.Ceiling(totalCount / (double)AdminPageSize);

    private sealed record QueryPage<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int TotalPages,
        int CurrentPage,
        int Offset);
}

public sealed record AdminPageResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int TotalPages,
    int CurrentPage,
    int PageSize,
    int Offset);

public sealed record VehiclePageResult(
    IReadOnlyList<VehicleListItem> Items,
    int TotalCount,
    int TotalPages,
    int CurrentPage,
    int PageSize,
    int Offset,
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
    [property: JsonPropertyName("korisnikEmail")] string UserEmail,
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
    [property: JsonPropertyName("kubikaza")] decimal? EngineDisplacement,
    [property: JsonPropertyName("dostupnoZaKupovinu")] bool IsAvailableForPurchase);

public sealed record ProfileListItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("userName")] string? UserName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("ime")] string FirstName,
    [property: JsonPropertyName("prezime")] string LastName,
    [property: JsonPropertyName("uloga")] string Role);
