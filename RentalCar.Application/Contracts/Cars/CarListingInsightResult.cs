namespace RentalCar.Application.Contracts.Cars;

/// <summary>LLM ile üretilen ilan içgörüsü (satın alma / kiralama).</summary>
public sealed class CarListingInsightResult
{
    public bool UsedLlm { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    /// <summary>Kısa cevap: bu araç alınır mı / dikkat edilmesi gerekenler.</summary>
    public string PurchaseVerdict { get; init; } = string.Empty;
    /// <summary>Fiyatın piyasaya göre konumu (ör. uygun / yüksek).</summary>
    public string PriceAnalysis { get; init; } = string.Empty;
    /// <summary>Önerinin gerekçesi (neden bu sonuç).</summary>
    public string RecommendationRationale { get; init; } = string.Empty;
    public IReadOnlyList<string> Bullets { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Alternatives { get; init; } = Array.Empty<string>();
}
