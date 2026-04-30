namespace RentalCar.Application.Contracts.Cars;

public sealed class CarBrandModelExportRow
{
    public string NormalizedKey { get; init; } = string.Empty;
    public string? CatalogBrand { get; init; }
    public string? Brand { get; init; }
    public string? CatalogModelName { get; init; }
    public string? Model { get; init; }
    public int CarCount { get; init; }
    public bool HasCatalogImage { get; init; }
    public int RemoteUrlCount { get; init; }
}
