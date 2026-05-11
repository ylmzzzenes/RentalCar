using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;

namespace RentalCar.Web;

public sealed class CarPriceInsightService : ICarPriceInsightService
{
    public Task<CarPriceInsightResult> AnalyzeAsync(
        Car car,
        IReadOnlyList<Car> comparableCars,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(car);

        var validComparableCars = comparableCars
            .Where(IsComparableWithPrice)
            .ToList();

        var listedPrice = car.ListedPrice;
        var predictedMin = car.PredictedPriceMin;
        var predictedMid = car.PredictedPriceMid;
        var predictedMax = car.PredictedPriceMax;

        var marketPrices = validComparableCars
            .Select(x => x.ListedPrice)
            .Where(x => x is > 0)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToList();

        decimal? marketAverage = marketPrices.Count > 0
            ? marketPrices.Average()
            : null;

        decimal? marketMedian = marketPrices.Count > 0
            ? CalculateMedian(marketPrices)
            : null;

        var benchmarkPrice = ResolveBenchmarkPrice(
            predictedMid,
            marketMedian,
            marketAverage,
            listedPrice);

        var differenceAmount = CalculateDifferenceAmount(listedPrice, benchmarkPrice);
        var differencePercent = CalculateDifferencePercent(listedPrice, benchmarkPrice);

        var label = ResolvePricePositionLabel(differencePercent);
        var labelTr = ResolvePricePositionLabelTr(label);

        var suggestedPrice = ResolveSuggestedPrice(predictedMid, marketMedian, marketAverage);

        var confidenceScore = CalculateConfidenceScore(
            predictedMid,
            validComparableCars.Count,
            marketAverage,
            marketMedian);

        var positiveFactors = BuildPositiveFactors(car, listedPrice, benchmarkPrice);
        var negativeFactors = BuildNegativeFactors(car, listedPrice, benchmarkPrice);
        var comparableNotes = BuildComparableCarNotes(validComparableCars);

        var summary = BuildSummary(listedPrice, benchmarkPrice, labelTr, differencePercent);

        var explanation = BuildExplanation(
            listedPrice,
            predictedMin,
            predictedMid,
            predictedMax,
            marketAverage,
            marketMedian,
            validComparableCars.Count,
            labelTr);

        var result = new CarPriceInsightResult
        {
            ListedPrice = listedPrice,
            PredictedMinPrice = predictedMin,
            PredictedMidPrice = predictedMid,
            PredictedMaxPrice = predictedMax,
            SuggestedPrice = suggestedPrice,
            MarketAveragePrice = marketAverage,
            MarketMedianPrice = marketMedian,
            DifferenceAmount = differenceAmount,
            DifferencePercent = differencePercent,
            PricePositionLabel = label,
            PricePositionLabelTr = labelTr,
            ConfidenceScore = confidenceScore,
            ComparableCarsCount = validComparableCars.Count,
            Summary = summary,
            Explanation = explanation,
            PositiveFactors = positiveFactors,
            NegativeFactors = negativeFactors,
            ComparableCarNotes = comparableNotes
        };

        return Task.FromResult(result);
    }

    private static bool IsComparableWithPrice(Car car)
        => car is not null && car.ListedPrice is > 0;

    private static decimal? CalculateMedian(IReadOnlyList<decimal> sortedPrices)
    {
        if (sortedPrices.Count == 0)
            return null;

        var mid = sortedPrices.Count / 2;

        if (sortedPrices.Count % 2 == 0)
            return (sortedPrices[mid - 1] + sortedPrices[mid]) / 2m;

        return sortedPrices[mid];
    }

    private static decimal? ResolveBenchmarkPrice(
        decimal? predictedMid,
        decimal? marketMedian,
        decimal? marketAverage,
        decimal? listedPrice)
    {
        if (predictedMid is > 0)
            return predictedMid;

        if (marketMedian is > 0)
            return marketMedian;

        if (marketAverage is > 0)
            return marketAverage;

        return listedPrice is > 0 ? listedPrice : null;
    }

    private static decimal? CalculateDifferenceAmount(decimal? listedPrice, decimal? benchmarkPrice)
    {
        if (listedPrice is not > 0 || benchmarkPrice is not > 0)
            return null;

        return listedPrice.Value - benchmarkPrice.Value;
    }

    private static decimal? CalculateDifferencePercent(decimal? listedPrice, decimal? benchmarkPrice)
    {
        if (listedPrice is not > 0 || benchmarkPrice is not > 0)
            return null;

        return ((listedPrice.Value - benchmarkPrice.Value) / benchmarkPrice.Value) * 100m;
    }

    private static string ResolvePricePositionLabel(decimal? differencePercent)
    {
        if (differencePercent is null)
            return "Unknown";

        if (differencePercent <= -10m)
            return "Underpriced";

        if (differencePercent >= 10m)
            return "Overpriced";

        return "Fair";
    }

    private static string ResolvePricePositionLabelTr(string label)
        => label switch
        {
            "Underpriced" => "Piyasanın altında",
            "Overpriced" => "Piyasanın üstünde",
            "Fair" => "Dengeli",
            _ => "Belirsiz"
        };

    private static decimal? ResolveSuggestedPrice(
        decimal? predictedMid,
        decimal? marketMedian,
        decimal? marketAverage)
    {
        var candidates = new[] { predictedMid, marketMedian, marketAverage }
            .Where(x => x is > 0)
            .Select(x => x!.Value)
            .ToList();

        if (candidates.Count == 0)
            return null;

        return Math.Round(candidates.Average(), 0);
    }

    private static int CalculateConfidenceScore(
        decimal? predictedMid,
        int comparableCarsCount,
        decimal? marketAverage,
        decimal? marketMedian)
    {
        var score = 35;

        if (predictedMid is > 0)
            score += 25;

        if (marketAverage is > 0)
            score += 10;

        if (marketMedian is > 0)
            score += 10;

        score += comparableCarsCount switch
        {
            >= 10 => 20,
            >= 5 => 15,
            >= 3 => 10,
            >= 1 => 5,
            _ => 0
        };

        return Math.Min(score, 100);
    }

    private static IReadOnlyList<string> BuildPositiveFactors(
        Car car,
        decimal? listedPrice,
        decimal? benchmarkPrice)
    {
        var factors = new List<string>();

        if (benchmarkPrice is > 0 && listedPrice is > 0 && listedPrice <= benchmarkPrice * 1.03m)
            factors.Add("İlan fiyatı referans değere yakın.");

        if (car.PredictedPriceMid is > 0)
            factors.Add("Model için yapay zekâ destekli fiyat tahmini mevcut.");

        if (car.PredictedPriceMin is > 0 && car.PredictedPriceMax is > 0)
            factors.Add("Tahmin tek sayı değil, fiyat bandı olarak hesaplanmış.");

        return factors;
    }

    private static IReadOnlyList<string> BuildNegativeFactors(
        Car car,
        decimal? listedPrice,
        decimal? benchmarkPrice)
    {
        var factors = new List<string>();

        if (benchmarkPrice is > 0 && listedPrice is > 0 && listedPrice >= benchmarkPrice * 1.10m)
            factors.Add("İlan fiyatı referans değerin belirgin şekilde üzerinde.");

        if (car.PredictedPriceMid is null or <= 0)
            factors.Add("Merkezi fiyat tahmini eksik.");

        if (car.PredictedPriceMin is null or <= 0 || car.PredictedPriceMax is null or <= 0)
            factors.Add("Tahmin bandı eksik veya zayıf.");

        return factors;
    }

    private static IReadOnlyList<string> BuildComparableCarNotes(IReadOnlyList<Car> comparableCars)
        => comparableCars
            .Take(5)
            .Select(x =>
                $"{(x.Brand ?? x.CatalogBrand ?? "-")} {(x.Model ?? x.CatalogModelName ?? "-")}, " +
                $"{x.ModelYear?.ToString() ?? "-"}, " +
                $"{x.OdometerKm?.ToString("N0") ?? "-"} km, " +
                $"{x.ListedPrice?.ToString("N0") ?? "-"} TL")
            .ToList();

    private static string BuildSummary(
        decimal? listedPrice,
        decimal? benchmarkPrice,
        string labelTr,
        decimal? differencePercent)
    {
        if (listedPrice is not > 0 || benchmarkPrice is not > 0)
            return "Bu ilan için yeterli fiyat kıyası üretilemedi.";

        var absPercent = Math.Round(Math.Abs(differencePercent ?? 0), 1);

        return $"{labelTr}. İlan fiyatı referans değere göre yaklaşık %{absPercent} fark gösteriyor.";
    }

    private static string BuildExplanation(
        decimal? listedPrice,
        decimal? predictedMin,
        decimal? predictedMid,
        decimal? predictedMax,
        decimal? marketAverage,
        decimal? marketMedian,
        int comparableCarsCount,
        string labelTr)
    {
        return
            $"İlan fiyatı {listedPrice?.ToString("N0") ?? "-"} TL. " +
            $"Tahmin bandı: {predictedMin?.ToString("N0") ?? "-"} TL - {predictedMax?.ToString("N0") ?? "-"} TL, " +
            $"merkez tahmin: {predictedMid?.ToString("N0") ?? "-"} TL. " +
            $"Benzer ilan sayısı: {comparableCarsCount}. " +
            $"Piyasa ortalaması: {marketAverage?.ToString("N0") ?? "-"} TL, " +
            $"medyan: {marketMedian?.ToString("N0") ?? "-"} TL. " +
            $"Genel sonuç: {labelTr}.";
    }
}