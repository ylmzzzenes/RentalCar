using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RentalCar.Application.Dtos.AI;

namespace RentalCar.Infrastructure.AI.Services;

public class PricingApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PricingApiClient> _logger;

    public PricingApiClient(HttpClient http, ILogger<PricingApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling AI pricing service.");
        var response = await _http.PostAsJsonAsync("/predict", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Tahmin API hatası: {(int)response.StatusCode} - {body}");

        var data = JsonSerializer.Deserialize<PredictResponseDto>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data is null)
            throw new InvalidOperationException("Tahmin API boş yanıt döndü.");

        data.Prediction ??= data.Mid;
        if (!data.Mid.HasValue && data.Prediction.HasValue)
            data.Mid = data.Prediction;

        return data;
    }
}
