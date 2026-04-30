using Microsoft.AspNetCore.Http;
using RentalCar.Domain.Enums;
using VehicleDriveType = RentalCar.Domain.Enums.DriveType;

namespace RentalCar.Web.Models.Requests;

public class CreateCar
{
    public string? CatalogBrand { get; set; }
    public string? Series { get; set; }
    public string? Model { get; set; }
    public int? ModelYear { get; set; }
    public int? OdometerKm { get; set; }
    public decimal? ListedPrice { get; set; }

    public Gear Transmission { get; set; }
    public FuelType FuelType { get; set; }
    public double? EngineDisplacementLiters { get; set; }
    public int? EnginePowerHp { get; set; }
    public VehicleDriveType Drivetrain { get; set; }

    public int? FuelTankLiters { get; set; }
    public BodyType ListingBodyType { get; set; }

    public string? Color { get; set; }
    public VehicleCondition VehicleCondition { get; set; }
    public string? BodyWorkNotes { get; set; }
    public bool TradeInAccepted { get; set; }
    public ListingSellerType SellerType { get; set; }

    public List<IFormFile>? PhotoUploads { get; set; }
}
