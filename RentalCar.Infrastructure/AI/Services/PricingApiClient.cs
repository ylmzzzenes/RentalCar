using System.Net.Http.Json;
using System.Text.Json;
using RentalCar.Application.Dtos.AI;

namespace RentalCar.Infrastructure.AI.Services;

public class PricingApiClient
{
    private readonly HttpClient _http;

    public PricingApiClient(HttpClient http) => _http = http;

    public async Task<PredictResponseDto> PredictAsync(PredictRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/predict", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Tahmin API hatası: {(int)response.StatusCode} - {body}");

        var data = JsonSerializer.Deserialize<PredictResponseDto>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data is null)
            throw new InvalidOperationException("Tahmin API boş yanıt döndü.");

        return data;
    }
}
