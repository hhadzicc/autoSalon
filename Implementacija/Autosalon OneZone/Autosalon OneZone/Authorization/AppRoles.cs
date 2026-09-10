namespace Autosalon_OneZone.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Seller = "Prodavac";
    public const string Buyer = "Kupac";
    public const string AdministratorOrSeller = Administrator + "," + Seller;

    public static readonly IReadOnlyList<string> All =
        new[] { Administrator, Seller, Buyer };
}
