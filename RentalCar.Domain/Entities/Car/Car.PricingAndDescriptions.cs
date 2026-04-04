using System.ComponentModel.DataAnnotations.Schema;

namespace RentalCar.Domain.Entities;

/// <summary>ML price estimates and listing copy.</summary>
public partial class Car
{
    [Column("fiyat_tahmin")]
    public decimal? PredictedPriceMid { get; set; }

    [Column("fiyat_min")]
    public decimal? PredictedPriceMin { get; set; }

    [Column("fiyat_max")]
    public decimal? PredictedPriceMax { get; set; }

    [Column("aciklama_kisa")]
    public string? ShortDescription { get; set; }

    [Column("aciklama")]
    public string? FullDescription { get; set; }

    public bool IsApproved { get; set; } = true;
}
