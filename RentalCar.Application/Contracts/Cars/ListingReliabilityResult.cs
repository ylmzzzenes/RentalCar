namespace RentalCar.Application.Contracts.Cars;

public sealed class ListingReliabilityResult
{
    /// <summary>0–100 nihai skor.</summary>
    public int Score { get; init; }

    /// <summary>İngilizce rozet kodu: Low, Medium, High.</summary>
    public string Label { get; init; } = "Medium";

    /// <summary>Kullanıcıya gösterilecek Türkçe güven etiketi.</summary>
    public string TrustLevelTr { get; init; } = "Orta güven";

    /// <summary>Kullanıcıya kısa gerekçe (1–3 cümle).</summary>
    public string UserExplanation { get; init; } = "";

    public IReadOnlyList<ListingReliabilityFactorDetail> Factors { get; init; } = Array.Empty<ListingReliabilityFactorDetail>();
}
