using RentalCar.Data.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RentalCar.Data.Services
{
    public class PricingApiClient
    {
        private readonly HttpClient _http;
        public PricingApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<PredictResponseDto> PredictAsync(PredictRequestDto req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/predict", req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"Python status: {(int)res.StatusCode} {res.StatusCode}");
            Console.WriteLine($"Python body: {body}");

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Python API error: {(int)res.StatusCode} - {body}");

            var data = JsonSerializer.Deserialize<PredictResponseDto>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null) throw new Exception("Python API boş response döndü");

            return data;
        }
    }
}
