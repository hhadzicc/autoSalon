namespace Autosalon_OneZone.Models.ViewModels;

public sealed class PaymentFieldsViewModel
{
    public string IdPrefix { get; init; } = string.Empty;
}

public sealed class LoanCalculatorViewModel
{
    public decimal? InitialAmount { get; init; }
    public int DefaultPeriod { get; init; } = 36;
    public decimal InterestRate { get; init; } = 5.0m;
}
