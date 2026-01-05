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
    public class DescriptionService
    {
        private readonly HttpClient _http;
        public DescriptionService(HttpClient http) => _http = http;

        public async Task<DescribeResponseDto> DescribeAsync(DescribeRequestDto req, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/describe", req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Python /describe error: {(int)res.StatusCode} - {body}");

            var data = JsonSerializer.Deserialize<DescribeResponseDto>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || string.IsNullOrWhiteSpace(data.@long))
                throw new Exception("Python /describe boş/eksik döndü");

            return data;
        }
    } 
}
