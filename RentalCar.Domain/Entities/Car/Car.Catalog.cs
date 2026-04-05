using System.ComponentModel.DataAnnotations.Schema;

namespace RentalCar.Domain.Entities;

/// <summary>Optional catalog / marketplace snapshot fields (legacy column names in the database).</summary>
public partial class Car
{
    [Column("modelraw")]
    public string? ModelRaw { get; set; }

    [Column("marka")]
    public string? CatalogBrand { get; set; }

    [Column("model_adi")]
    public string? CatalogModelName { get; set; }

    [Column("paket")]
    public string? TrimPackage { get; set; }

    [Column("motor_kodu")]
    public string? EngineCode { get; set; }

    [Column("sanziman_kodu")]
    public string? TransmissionCode { get; set; }

    [Column("kilometre")]
    public int? OdometerKm { get; set; }

    [Column("vergi")]
    public int? TaxAmount { get; set; }

    [Column("lt_100km")]
    public double? FuelConsumptionLPer100Km { get; set; }

    [Column("motorHacmi")]
    public double? EngineDisplacementLiters { get; set; }

    [Column("fiyat")]
    public decimal? ListedPrice { get; set; }

    [Column("sehir")]
    public string? City { get; set; }

    [Column("kasaTipi")]
    public string? BodyStyleLabel { get; set; }

    [Column("donanimSeviyesi")]
    public string? TrimLevelLabel { get; set; }

    [Column("hasarKaydi")]
    public int? HasAccidentRecord { get; set; }

    [Column("degisenBoyanan")]
    public string? BodyWorkNotes { get; set; }

    [Column("servisGecmisi")]
    public int? HasServiceHistory { get; set; }

    [Column("motorGuc_hp")]
    public int? EnginePowerHp { get; set; }

    [Column("tork_nm")]
    public int? TorqueNm { get; set; }

    [Column("sahipSayisi")]
    public int? PreviousOwnerCount { get; set; }
}
