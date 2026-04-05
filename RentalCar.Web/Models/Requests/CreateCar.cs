using Microsoft.AspNetCore.Http;
using RentalCar.Domain.Enums;
using VehicleDriveType = RentalCar.Domain.Enums.DriveType;

namespace RentalCar.Web.Models.Requests;

public class CreateCar
{
    public int Id { get; set; }
    public string? Vin { get; set; }
    public string? Brand { get; set; }
    public string? Plate { get; set; }
    public string? Model { get; set; }
    public int? ModelYear { get; set; }
    public FuelType FuelType { get; set; }
    public Gear Transmission { get; set; }
    public VehicleDriveType Drivetrain { get; set; }
    public string? Color { get; set; }
    public string? ModelRaw { get; set; }
    public string? CatalogBrand { get; set; }
    public string? CatalogModelName { get; set; }
    public string? TrimPackage { get; set; }
    public string? EngineCode { get; set; }
    public string? TransmissionCode { get; set; }
    public int? OdometerKm { get; set; }
    public int? TaxAmount { get; set; }
    public double? FuelConsumptionLPer100Km { get; set; }
    public double? EngineDisplacementLiters { get; set; }
    public decimal? ListedPrice { get; set; }
    public string? City { get; set; }
    public string? BodyStyleLabel { get; set; }
    public string? TrimLevelLabel { get; set; }
    public int? HasAccidentRecord { get; set; }
    public string? BodyWorkNotes { get; set; }
    public int? HasServiceHistory { get; set; }
    public int? EnginePowerHp { get; set; }
    public int? TorqueNm { get; set; }
    public int? PreviousOwnerCount { get; set; }
    public List<IFormFile>? PhotoUploads { get; set; }
}
