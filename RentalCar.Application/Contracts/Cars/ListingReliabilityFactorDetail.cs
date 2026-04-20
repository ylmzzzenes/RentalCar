namespace RentalCar.Application.Contracts.Cars;

/// <summary>Debug / şeffaflık için tek bir skor bileşeni.</summary>
public sealed class ListingReliabilityFactorDetail
{
    public string Code { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public double Weight { get; init; }
    /// <summary>0–100 alt skor.</summary>
    public double Subscore { get; init; }
    public double WeightedContribution { get; init; }
    public string? Note { get; init; }
}
