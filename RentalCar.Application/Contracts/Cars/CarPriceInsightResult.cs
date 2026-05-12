namespace RentalCar.Application.Contracts.Cars
{
    public sealed class CarPriceInsightResult
    {
        public decimal? ListedPrice { get; init; }

        public decimal? PredictedMinPrice { get; init; }
        public decimal? PredictedMidPrice { get; init; }
        public decimal? PredictedMaxPrice { get; init; }

        public decimal? SuggestedPrice { get; init; }

        public decimal? MarketAveragePrice { get; init; }
        public decimal? MarketMedianPrice { get; init; }

        public decimal? DifferenceAmount { get; init; }
        public decimal? DifferencePercent { get; init; }

        public string PricePositionLabel { get; init; } = "Unknown";
        public string PricePositionLabelTr { get; init; } = "Belirsiz";

        public int ConfidenceScore { get; init; }

        public int ComparableCarsCount { get; init; }

        public string Summary { get; init; } = string.Empty;
        public string Explanation { get; init; } = string.Empty;

        public IReadOnlyList<string> PositiveFactors { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> NegativeFactors { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> ComparableCarNotes { get; init; } = Array.Empty<string>();
    }
}
