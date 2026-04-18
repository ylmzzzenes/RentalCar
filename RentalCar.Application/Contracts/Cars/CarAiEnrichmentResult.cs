namespace RentalCar.Application.Contracts.Cars;

public sealed class CarAiEnrichmentResult
{
    public decimal MidPrice { get; init; }
    public decimal LowPrice { get; init; }
    public decimal HighPrice { get; init; }
    public string ShortDescription { get; init; } = string.Empty;
    public string FullDescription { get; init; } = string.Empty;
    public List<string> ImageUrls { get; init; } = new();
}
