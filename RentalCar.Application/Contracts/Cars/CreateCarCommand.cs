using RentalCar.Domain.Enums;
using DriveType = RentalCar.Domain.Enums.DriveType;

namespace RentalCar.Application.Contracts.Cars;

public class CreateCarCommand
{
    public string? Model { get; set; }
    public int? ModelYear { get; set; }
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
    public decimal ListedPrice { get; set; }
    public string? City { get; set; }
    public string? TrimLevelLabel { get; set; }
    public int HasAccidentRecord { get; set; }
    public int HasServiceHistory { get; set; }
    public int? EnginePowerHp { get; set; }
    public int? TorqueNm { get; set; }
    public int? PreviousOwnerCount { get; set; }
    public string? BodyStyleLabel { get; set; }
    public string? BodyWorkNotes { get; set; }

    public FuelType FuelType { get; set; }
    public Gear Transmission { get; set; }
    public DriveType Drivetrain { get; set; }

    public List<int>? SelectedSecurity { get; set; }
    public List<int>? SelectedInternal { get; set; }
    public List<int>? SelectedExternal { get; set; }
    public List<int>? SelectedBodyType { get; set; }
    public List<int>? SelectedFuelType { get; set; }
    public List<int>? SelectedGear { get; set; }
    public List<int>? SelectedDriveType { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}