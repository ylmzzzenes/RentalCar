using System.ComponentModel.DataAnnotations.Schema;
using RentalCar.Domain.Enums;

namespace RentalCar.Domain.Entities;

/// <summary>Core rental listing fields (identity, powertrain, pricing, media).</summary>
public partial class Car
{
    public string? Vin { get; set; }
    public string? Brand { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    public int? ModelYear { get; set; }

    [Column("yakitTuru")]
    public FuelType FuelType { get; set; }

    [Column("vites")]
    public Gear Transmission { get; set; }

    public BodyType BodyType { get; set; }

    [Column("renk")]
    public string? Color { get; set; }

    public string? Plate { get; set; }

    public Security Security { get; set; }
    public InternalEquipment InternalEquipment { get; set; }
    public ExternalEquipment ExternalEquipment { get; set; }

    public decimal DailyPrice { get; set; }
    public decimal WeeklyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }

    [Column("cekis")]
    public global::RentalCar.Domain.Enums.DriveType Drivetrain { get; set; }

    /// <summary>Image file names or URLs; persisted as a single delimited column via EF conversion.</summary>
    public List<string> ImageUrls { get; set; } = new();
}
