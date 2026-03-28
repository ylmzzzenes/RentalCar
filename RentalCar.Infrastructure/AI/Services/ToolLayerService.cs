using System.Text.Json;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Data.Enums;

namespace RentalCar.AI.Services
{
    public class ToolLayerService : IToolLayerService
    {
        private readonly IRentalService _rentalService;
        private readonly IRecommendationService _recommendationService;
        private readonly IPricingService _pricingService;
        private readonly IFaqService _faqService;

        public ToolLayerService(
            IRentalService rentalService,
            IRecommendationService recommendationService,
            IPricingService pricingService,
            IFaqService faqService)
        {
            _rentalService = rentalService;
            _recommendationService = recommendationService;
            _pricingService = pricingService;
            _faqService = faqService;
        }

        public string BuildToolDefinitionJson()
        {
            var tools = new object[]
            {
                BuildTool("search_cars", "Sehir, fiyat, arac tipi ve yakit kriterleriyle arac arar.", new
                {
                    type = "object",
                    properties = new
                    {
                        city = new { type = "string" },
                        start_date = new { type = "string", description = "YYYY-MM-DD" },
                        end_date = new { type = "string", description = "YYYY-MM-DD" },
                        min_price = new { type = "number" },
                        max_price = new { type = "number" },
                        vehicle_type = new { type = "string" },
                        fuel_type = new { type = "string" }
                    }
                }),
                BuildTool("get_car_detail", "Arac detayini getirir.", new
                {
                    type = "object",
                    properties = new
                    {
                        car_id = new { type = "integer" }
                    },
                    required = new[] { "car_id" }
                }),
                BuildTool("recommend_cars", "Kullanici tercihine gore arac onerir.", new
                {
                    type = "object",
                    properties = new
                    {
                        city = new { type = "string" },
                        preference = new { type = "string" },
                        fuel_type = new { type = "string" },
                        max_price = new { type = "number" }
                    }
                }),
                BuildTool("calculate_rental_price", "Kiralama bedeli ve en uygun plani hesaplar.", new
                {
                    type = "object",
                    properties = new
                    {
                        car_id = new { type = "integer" },
                        rental_type = new { type = "string", description = "Daily, Weekly, Monthly, LongTerm" },
                        duration = new { type = "number" }
                    },
                    required = new[] { "car_id", "rental_type", "duration" }
                }),
                BuildTool("faq_search", "Sik sorulan sorularda cevap arar.", new
                {
                    type = "object",
                    properties = new
                    {
                        question = new { type = "string" }
                    },
                    required = new[] { "question" }
                })
            };

            return JsonSerializer.Serialize(tools);
        }

        public async Task<ToolExecutionResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            switch (toolName)
            {
                case "search_cars":
                {
                    var city = GetString(root, "city");
                    var vehicleType = GetString(root, "vehicle_type");
                    var fuelType = GetString(root, "fuel_type");
                    var minPrice = GetDecimal(root, "min_price");
                    var maxPrice = GetDecimal(root, "max_price");

                    var cars = await _rentalService.SearchCarsAsync(city, vehicleType, minPrice, maxPrice, fuelType, cancellationToken);
                    var payload = new { count = cars.Count, cars };

                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        Cars = cars,
                        SuggestedFilters = new ChatFilterSuggestion
                        {
                            City = city,
                            VehicleType = vehicleType,
                            MinPrice = minPrice,
                            MaxPrice = maxPrice,
                            FuelType = fuelType
                        },
                        ResultJson = JsonSerializer.Serialize(payload)
                    };
                }
                case "get_car_detail":
                {
                    var carId = GetInt(root, "car_id");
                    var detail = await _rentalService.GetCarDetailAsync(carId, cancellationToken);
                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        ResultJson = JsonSerializer.Serialize(detail)
                    };
                }
                case "recommend_cars":
                {
                    var city = GetString(root, "city");
                    var preference = GetString(root, "preference");
                    var fuelType = GetString(root, "fuel_type");
                    var maxPrice = GetDecimal(root, "max_price");
                    var cars = await _recommendationService.RecommendCarsAsync(city, preference, fuelType, maxPrice, cancellationToken);
                    var payload = new { count = cars.Count, cars };

                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        Cars = cars,
                        ResultJson = JsonSerializer.Serialize(payload)
                    };
                }
                case "calculate_rental_price":
                {
                    var carId = GetInt(root, "car_id");
                    var rentalTypeText = GetString(root, "rental_type") ?? "Daily";
                    var rentalType = Enum.TryParse<RentalType>(rentalTypeText, true, out var parsedType) ? parsedType : RentalType.Daily;
                    var duration = GetDecimal(root, "duration") ?? 1;
                    var pricing = await _pricingService.CalculateRentalPriceAsync(carId, rentalType, duration, cancellationToken);

                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        ResultJson = JsonSerializer.Serialize(pricing)
                    };
                }
                case "faq_search":
                {
                    var question = GetString(root, "question") ?? string.Empty;
                    var faq = _faqService.Search(question);
                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        ResultJson = JsonSerializer.Serialize(faq)
                    };
                }
                default:
                    return new ToolExecutionResult
                    {
                        ToolName = toolName,
                        ResultJson = JsonSerializer.Serialize(new { error = "Unknown tool." })
                    };
            }
        }

        private static object BuildTool(string name, string description, object parameters)
            => new
            {
                type = "function",
                function = new
                {
                    name,
                    description,
                    parameters
                }
            };

        private static string? GetString(JsonElement root, string name)
            => root.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.String ? node.GetString() : null;

        private static int GetInt(JsonElement root, string name)
            => root.TryGetProperty(name, out var node) && node.TryGetInt32(out var result) ? result : 0;

        private static decimal? GetDecimal(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var node)) return null;
            if (node.ValueKind == JsonValueKind.Number && node.TryGetDecimal(out var d)) return d;
            if (node.ValueKind == JsonValueKind.String && decimal.TryParse(node.GetString(), out var p)) return p;
            return null;
        }
    }
}
