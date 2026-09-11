namespace Autosalon_OneZone.Models.ViewModels;

public sealed class VehicleListingViewModel
{
    public required IReadOnlyList<VehicleCardViewModel> Vehicles { get; init; }
    public required int TotalCount { get; init; }
    public required int CurrentPage { get; init; }
    public required bool HasMore { get; init; }
    public string? SearchTerm { get; init; }
}

public sealed class VehicleCardViewModel
{
    public required Vozilo Vehicle { get; init; }
    public required bool IsInCart { get; init; }
}
