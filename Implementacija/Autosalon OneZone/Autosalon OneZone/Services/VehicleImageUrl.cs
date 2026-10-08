namespace Autosalon_OneZone.Services;

public static class VehicleImageUrl
{
    public static string Detail(int vehicleId, string detailFileName) =>
        Build(vehicleId, "detail", detailFileName);

    public static string Thumbnail(int vehicleId, string detailFileName) =>
        Build(vehicleId, "thumbnail", VehicleImageNames.ThumbnailFor(detailFileName));

    private static string Build(int vehicleId, string variant, string version) =>
        $"/vehicle-images/{vehicleId}/{variant}?v={Uri.EscapeDataString(version)}";
}
