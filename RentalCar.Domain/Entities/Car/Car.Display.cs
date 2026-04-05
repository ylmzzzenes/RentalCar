namespace RentalCar.Domain.Entities;

public partial class Car
{
    public string DisplayTitle =>
        $"{CatalogBrand ?? Brand ?? ""} {CatalogModelName ?? Model ?? ""}".Trim();
}
