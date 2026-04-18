using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RentalCar.Application.Dtos.AI;

namespace RentalCar.Infrastructure.AI.Services;

public class DescriptionService
{
    private readonly HttpClient _http;
    private readonly ILogger<DescriptionService> _logger;

    public DescriptionService(HttpClient http, ILogger<DescriptionService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<DescribeResponseDto> DescribeAsync(DescribeRequestDto req, CancellationToken ct = default)
    {
        _logger.LogInformation("Calling AI description service.");
        var res = await _http.PostAsJsonAsync("/describe", req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Python /describe error: {(int)res.StatusCode} - {body}");

        var data = JsonSerializer.Deserialize<DescribeResponseDto>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data == null || string.IsNullOrWhiteSpace(data.@long))
            throw new InvalidOperationException("Python /describe boş/eksik döndü");

        data.@short = string.IsNullOrWhiteSpace(data.@short)
            ? "AI destekli ilan ozeti hazirlandi."
            : data.@short.Trim();
        data.@long = data.@long.Trim();

        return data;
    }
}
