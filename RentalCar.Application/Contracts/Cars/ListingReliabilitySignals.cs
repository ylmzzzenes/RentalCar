namespace RentalCar.Application.Contracts.Cars;

/// <summary>DB ve toplu analizden gelen bağlam; skor motoruna girdi.</summary>
public sealed class ListingReliabilitySignals
{
    public int CarId { get; init; }

    /// <summary>Aynı marka/model/yıl ve yakın fiyata sahip diğer onaylı ilan sayısı (bu ilan hariç).</summary>
    public int SimilarListingCount { get; init; }

    /// <summary>Aynı satıcıya ait toplam onaylı ilan (bu ilan dahil, kayıtlıysa).</summary>
    public int SellerListingCount { get; init; }

    /// <summary>Satıcının ilanlarına verilen puanların ortalaması (CarRating.Score), örnek yoksa null.</summary>
    public double? SellerAverageCarRating { get; init; }

    public int SellerRatingSampleSize { get; init; }

    /// <summary>Aynı segmentteki (marka/model/yıl) diğer ilanların ortalama fiyatı; tahmin yoksa kullanılır.</summary>
    public decimal? PeerAverageListedPrice { get; init; }

    public int PeerPriceSampleSize { get; init; }
}
