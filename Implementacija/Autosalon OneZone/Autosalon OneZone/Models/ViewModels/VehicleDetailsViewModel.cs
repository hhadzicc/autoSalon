namespace Autosalon_OneZone.Models.ViewModels;

public sealed class VehicleDetailsViewModel
{
    public required Vozilo Vehicle { get; init; }
    public CustomerExperienceSummaryViewModel CustomerExperiences { get; init; } = new();
}

public sealed class CustomerExperienceSummaryViewModel
{
    public int TotalCount { get; init; }
    public double? AverageRating { get; init; }
    public IReadOnlyList<CustomerExperienceViewModel> Recent { get; init; } = [];
}

public sealed class CustomerExperienceViewModel
{
    public required string CustomerName { get; init; }
    public required string VehicleName { get; init; }
    public int Rating { get; init; }
    public required string Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
