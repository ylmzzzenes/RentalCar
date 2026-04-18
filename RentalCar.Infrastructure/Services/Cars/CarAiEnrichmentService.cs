using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Dtos.AI;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RentalCar.AI.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RentalCar.Infrastructure.AI.Services;

namespace RentalCar.Infrastructure.Services.Cars;

public class CarAiEnrichmentService : ICarAiEnrichmentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<CarAiEnrichmentService> _logger;
    private readonly PricingApiClient _pricingApiClient;
    private readonly DescriptionService _descriptionService;

    public CarAiEnrichmentService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<CarAiEnrichmentService> logger,
        PricingApiClient pricingApiClient,
        DescriptionService descriptionService)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
        _pricingApiClient = pricingApiClient;
        _descriptionService = descriptionService;
    }

    public async Task<CarAiEnrichmentResult> EnrichAsync(Car car, CancellationToken cancellationToken = default)
    {
        var fallbackMid = CalculateFallbackPrice(car);
        var fallbackLow = Math.Round(fallbackMid * 0.9m, 0);
        var fallbackHigh = Math.Round(fallbackMid * 1.1m, 0);

        var mid = fallbackMid;
        var low = fallbackLow;
        var high = fallbackHigh;

        try
        {
            var llm = await TryGetLlmPriceAsync(car, cancellationToken);
            if (llm is not null)
            {
                mid = llm.Value.mid;
                low = llm.Value.low;
                high = llm.Value.high;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM fiyat tahmini basarisiz. Pricing API fallback uygulanacak.");
        }

        if (mid == fallbackMid && low == fallbackLow && high == fallbackHigh)
        {
            try
            {
                var predictRequest = MapCarToPredict(car);
                var predict = await _pricingApiClient.PredictAsync(predictRequest, cancellationToken);
                mid = predict.Mid ?? predict.Prediction ?? fallbackMid;
                low = predict.Low ?? Math.Round(mid * 0.9m, 0);
                high = predict.High ?? Math.Round(mid * 1.1m, 0);
            }
            catch
            {
                // AI servisinde hata varsa fallback fiyatla devam ediyoruz.
            }
        }

        var shortDescription = BuildFallbackShortDescription(car, mid);
        var fullDescription = BuildFallbackLongDescription(car);

        try
        {
            var describeRequest = new DescribeRequestDto
            {
                data = new Dictionary<string, object?>
                {
                    ["marka"] = car.CatalogBrand ?? car.Brand,
                    ["seri"] = car.Series,
                    ["model"] = car.CatalogModelName ?? car.Model,
                    ["yil"] = car.ModelYear,
                    ["kilometre"] = car.OdometerKm,
                    ["yakit"] = car.FuelType.ToString(),
                    ["vites"] = car.Transmission.ToString(),
                    ["renk"] = car.Color,
                    ["sehir"] = car.City,
                    ["yakit_deposu"] = car.FuelTankLiters,
                    ["arac_durumu"] = car.VehicleCondition.ToString(),
                    ["kimden"] = car.SellerType.ToString(),
                    ["takas"] = car.TradeInAccepted
                },
                predicted_mid = mid,
                predicted_low = low,
                predicted_high = high
            };

            var desc = await _descriptionService.DescribeAsync(describeRequest, cancellationToken);
            if (!string.IsNullOrWhiteSpace(desc.@short))
                shortDescription = desc.@short.Trim();
            if (!string.IsNullOrWhiteSpace(desc.@long))
                fullDescription = desc.@long.Trim();
        }
        catch
        {
            // AI açıklama üretemezse fallback metinle devam.
        }

        return new CarAiEnrichmentResult
        {
            MidPrice = mid,
            LowPrice = low,
            HighPrice = high,
            ShortDescription = shortDescription,
            FullDescription = fullDescription,
            ImageUrls = BuildCatalogMatchedImageUrls(car)
        };
    }

    private static PredictRequestDto MapCarToPredict(Car car)
    {
        return new PredictRequestDto
        {
            marka = car.CatalogBrand ?? car.Brand ?? "Bilinmiyor",
            model_adi = car.CatalogModelName ?? car.Model ?? "Bilinmiyor",
            paket = car.Series ?? car.TrimPackage ?? "Standard",
            motor_kodu = car.EngineCode ?? "Bilinmiyor",
            cekis = car.Drivetrain.ToString(),
            sanziman_kodu = car.TransmissionCode ?? "Bilinmiyor",
            vites = car.Transmission.ToString(),
            yakitTuru = car.FuelType.ToString(),
            renk = car.Color ?? "Bilinmiyor",
            sehir = car.City,
            kasaTipi = car.BodyStyleLabel ?? car.BodyType.ToString(),
            donanimSeviyesi = car.TrimLevelLabel,
            hasarKaydi = car.HasAccidentRecord,
            degisenBoyanan = car.BodyWorkNotes,
            servisGecmisi = car.HasServiceHistory,
            motorGuc_hp = car.EnginePowerHp,
            tork_nm = car.TorqueNm,
            sahipSayisi = car.PreviousOwnerCount,
            yil = car.ModelYear ?? 0,
            kilometre = car.OdometerKm ?? 0,
            vergi = car.TaxAmount ?? 0,
            lt_100km = car.FuelConsumptionLPer100Km ?? 0.0,
            motorHacmi = car.EngineDisplacementLiters ?? 0.0
        };
    }

    private static decimal CalculateFallbackPrice(Car car)
    {
        if (car.ListedPrice.HasValue && car.ListedPrice.Value > 0)
            return car.ListedPrice.Value;

        var basePrice = 650_000m;
        if (car.ModelYear.HasValue)
        {
            var age = Math.Max(0, DateTime.UtcNow.Year - car.ModelYear.Value);
            basePrice -= age * 18_000m;
        }

        if (car.OdometerKm.HasValue)
        {
            basePrice -= Math.Max(0, car.OdometerKm.Value - 30_000) * 1.2m;
        }

        if (car.VehicleCondition == VehicleCondition.HasarKayitli)
        {
            basePrice *= 0.92m;
        }

        return Math.Max(200_000m, Math.Round(basePrice, 0));
    }

    private static string BuildFallbackShortDescription(Car car, decimal midPrice)
    {
        var brand = car.CatalogBrand ?? car.Brand ?? "Arac";
        var model = car.CatalogModelName ?? car.Model ?? string.Empty;
        var year = car.ModelYear?.ToString() ?? "-";
        return $"{brand} {model} {year} icin AI onerilen fiyat: {midPrice:N0} TL.";
    }

    private static string BuildFallbackLongDescription(Car car)
    {
        var brand = car.CatalogBrand ?? car.Brand ?? "Arac";
        var model = car.CatalogModelName ?? car.Model ?? "model";
        var year = car.ModelYear?.ToString() ?? "-";
        var km = car.OdometerKm?.ToString("N0") ?? "-";
        var fuel = car.FuelType.ToString();
        var gear = car.Transmission.ToString();
        var city = string.IsNullOrWhiteSpace(car.City) ? "belirtilmedi" : car.City;

        return $"{brand} {model} ({year}) ilaninda {km} km, {fuel} yakit ve {gear} vites bilgileri yer aliyor. " +
               $"Arac {city} lokasyonunda listelenmis olup, detayli ekspertiz ve test surusu ile son karar verilmesi onerilir.";
    }

    private static List<string> BuildCatalogMatchedImageUrls(Car car)
    {
        var key = CarCatalogImageLibrary.BuildLookupKey(car.CatalogBrand, car.Brand, car.CatalogModelName, car.Model);
        var remote = CarCatalogImageLibrary.TryGetRemoteUrls(key);
        return remote is null ? new List<string>() : remote.ToList();
    }

    private async Task<(decimal mid, decimal low, decimal high)?> TryGetLlmPriceAsync(Car car, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey))
            return null;

        var prompt =
            $@"Arac ilan fiyat tahmini yap. Sadece gecerli JSON don.
Alanlar:
- marka: {car.CatalogBrand ?? car.Brand ?? "Bilinmiyor"}
- model: {car.CatalogModelName ?? car.Model ?? "Bilinmiyor"}
- yil: {car.ModelYear?.ToString() ?? "null"}
- km: {car.OdometerKm?.ToString() ?? "null"}
- yakit: {car.FuelType}
- vites: {car.Transmission}
- sehir: {car.City ?? "Bilinmiyor"}
- hasar: {car.HasAccidentRecord?.ToString() ?? "null"}
- liste_fiyat: {car.ListedPrice?.ToString() ?? "null"}

Sadece su JSON formatinda don:
{{""mid"":number,""low"":number,""high"":number}}
Kurallar:
- low <= mid <= high
- tum sayilar pozitif olsun
- TL bazinda tam sayi uret";

        var payload = new
        {
            model = _openAiOptions.Model,
            temperature = 0.1,
            messages = new object[]
            {
                new { role = "system", content = "Sen otomotiv fiyatlama asistanisin. Yalnizca gecerli JSON don." },
                new { role = "user", content = prompt }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _openAiOptions.TimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_openAiOptions.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var jsonText = content.Trim();
        var start = jsonText.IndexOf('{');
        var end = jsonText.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        jsonText = jsonText[start..(end + 1)];
        using var parsed = JsonDocument.Parse(jsonText);
        var root = parsed.RootElement;
        if (!root.TryGetProperty("mid", out var midNode) || !midNode.TryGetDecimal(out var mid))
            return null;
        if (!root.TryGetProperty("low", out var lowNode) || !lowNode.TryGetDecimal(out var low))
            low = Math.Round(mid * 0.9m, 0);
        if (!root.TryGetProperty("high", out var highNode) || !highNode.TryGetDecimal(out var high))
            high = Math.Round(mid * 1.1m, 0);

        mid = Math.Max(1, Math.Round(mid, 0));
        low = Math.Max(1, Math.Round(Math.Min(low, mid), 0));
        high = Math.Max(mid, Math.Round(high, 0));
        return (mid, low, high);
    }
}
