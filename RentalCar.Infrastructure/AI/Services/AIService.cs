using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RentalCar.Infrastructure.AI.Models;
using RentalCar.AI.Configuration;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;
using RentalCar.Application.AI.Responses;

namespace RentalCar.AI.Services
{
    public class AIService : IAIService
    {
        private const int MaxToolRoundtrip = 2;
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;
        private readonly IIntentClassifier _intentClassifier;
        private readonly IConversationMemoryService _memoryService;
        private readonly IToolLayerService _toolLayerService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;

        public AIService(
            HttpClient httpClient,
            IOptions<OpenAiOptions> options,
            IIntentClassifier intentClassifier,
            IConversationMemoryService memoryService,
            IToolLayerService toolLayerService,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _intentClassifier = intentClassifier;
            _memoryService = memoryService;
            _toolLayerService = toolLayerService;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<AiCompletionResult> ReplyAsync(string userMessage, string sessionId, CancellationToken cancellationToken = default)
        {
            var intent = _intentClassifier.Classify(userMessage);
            ValidateInput(userMessage);
            EnsureRateLimit();

            await _memoryService.AppendAsync(sessionId, "user", userMessage, cancellationToken);

            var history = await _memoryService.GetMessagesAsync(sessionId, cancellationToken);
            var messageList = BuildMessages(history, userMessage);

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                var fallback = await BuildToolOnlyFallbackAsync(intent, userMessage, cancellationToken);
                await _memoryService.AppendAsync(sessionId, "assistant", fallback.Message, cancellationToken);
                fallback.Intent = intent;
                return fallback;
            }

            var toolDefsJson = _toolLayerService.BuildToolDefinitionJson();
            var firstPass = await CreateChatCompletionAsync(messageList, toolDefsJson, true, cancellationToken);

            var collectedCars = new List<ChatCarCard>();
            ChatFilterSuggestion? suggestedFilters = null;
            var answer = firstPass.Content;

            var current = firstPass;
            var round = 0;
            while (current.ToolCalls.Count > 0 && round < MaxToolRoundtrip)
            {
                round++;
                var assistantWithToolCalls = new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = current.Content
                };

                var toolCallsJson = new JsonArray();
                var toolMessages = new List<JsonObject>();
                foreach (var toolCall in current.ToolCalls)
                {
                    var exec = await _toolLayerService.ExecuteAsync(toolCall.Name, toolCall.Arguments.GetRawText(), cancellationToken);
                    if (exec.Cars.Count > 0) collectedCars = exec.Cars;
                    if (exec.SuggestedFilters is not null) suggestedFilters = exec.SuggestedFilters;

                    toolCallsJson.Add(new JsonObject
                    {
                        ["id"] = toolCall.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = toolCall.Name,
                            ["arguments"] = toolCall.Arguments.GetRawText()
                        }
                    });

                    toolMessages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = toolCall.Id,
                        ["content"] = exec.ResultJson
                    });
                }

                assistantWithToolCalls["tool_calls"] = toolCallsJson;
                messageList.Add(assistantWithToolCalls);
                foreach (var toolMessage in toolMessages)
                {
                    messageList.Add(toolMessage);
                }

                current = await CreateChatCompletionAsync(messageList, toolDefsJson, true, cancellationToken);
                answer = current.Content;
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                answer = "Talebinizi isledim. Ilanlari filtreleyip uygun secenekleri inceleyebilirsiniz.";
            }

            await _memoryService.AppendAsync(sessionId, "assistant", answer, cancellationToken);

            return new AiCompletionResult
            {
                Message = answer,
                Intent = intent,
                Cars = collectedCars,
                SuggestedFilters = suggestedFilters
            };
        }

        private static void ValidateInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException("Mesaj bos olamaz.");
            if (message.Length > 1000)
                throw new InvalidOperationException("Mesaj en fazla 1000 karakter olabilir.");
        }

        private void EnsureRateLimit()
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"ai-rate:{ip}";
            var now = DateTime.UtcNow;
            var window = TimeSpan.FromMinutes(1);
            var maxRequests = 20;

            var timestamps = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return new List<DateTime>();
            })!;

            lock (timestamps)
            {
                timestamps.RemoveAll(t => now - t > window);
                if (timestamps.Count >= maxRequests)
                    throw new InvalidOperationException("Cok fazla istek gonderildi. Lutfen 1 dakika sonra tekrar deneyin.");
                timestamps.Add(now);
            }
        }

        private JsonArray BuildMessages(List<ConversationMessage> history, string userMessage)
        {
            var messages = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] =
                        "Sen RentalCar AI Assistant'sin. Net, kisa ve anlasilir cevap ver. " +
                        "Uydurma bilgi verme; emin degilsen bunu acikca belirt. " +
                        "Arac arama, fiyatlama, kiralama plani ve SSS icin gereken durumda tool cagir. " +
                        "Tool sonucu olmadan kesin fiyat/uygunluk iddiasi kurma."
                }
            };

            foreach (var item in history.TakeLast(10))
            {
                messages.Add(new JsonObject
                {
                    ["role"] = item.Role,
                    ["content"] = item.Content
                });
            }

            messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = userMessage
            });

            return messages;
        }

        private async Task<OpenAiResponse> CreateChatCompletionAsync(
            JsonArray messages,
            string toolsJson,
            bool allowTools,
            CancellationToken cancellationToken)
        {
            var payload = new JsonObject
            {
                ["model"] = _options.Model,
                ["temperature"] = 0.2,
                ["messages"] = messages
            };

            if (allowTools)
            {
                payload["tools"] = JsonNode.Parse(toolsJson);
                payload["tool_choice"] = "auto";
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI istegi basarisiz: {(int)response.StatusCode} - {raw}");
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var first = root.GetProperty("choices")[0].GetProperty("message");
            var content = first.TryGetProperty("content", out var contentNode) && contentNode.ValueKind == JsonValueKind.String
                ? contentNode.GetString() ?? string.Empty
                : string.Empty;

            var toolCalls = new List<OpenAiToolCall>();
            if (first.TryGetProperty("tool_calls", out var toolNode) && toolNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in toolNode.EnumerateArray())
                {
                    var function = tc.GetProperty("function");
                    var args = function.GetProperty("arguments").GetString() ?? "{}";
                    using var argsDoc = JsonDocument.Parse(args);
                    toolCalls.Add(new OpenAiToolCall
                    {
                        Id = tc.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
                        Name = function.GetProperty("name").GetString() ?? string.Empty,
                        Arguments = argsDoc.RootElement.Clone()
                    });
                }
            }

            return new OpenAiResponse
            {
                Content = content,
                ToolCalls = toolCalls
            };
        }

        private async Task<AiCompletionResult> BuildToolOnlyFallbackAsync(string intent, string userMessage, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "car_search":
                {
                    var result = await _toolLayerService.ExecuteAsync(
                        "search_cars",
                        JsonSerializer.Serialize(new
                        {
                            city = ExtractCity(userMessage),
                            vehicle_type = ExtractVehicleType(userMessage),
                            min_price = ExtractMinPrice(userMessage),
                            max_price = ExtractMaxPrice(userMessage),
                            fuel_type = ExtractFuelType(userMessage)
                        }),
                        cancellationToken);

                    return new AiCompletionResult
                    {
                        Message = result.Cars.Count > 0
                            ? $"{result.Cars.Count} uygun arac buldum. Kartlardan inceleyebilirsin."
                            : "Bu kriterlerde arac bulamadim, filtreyi biraz genisletelim.",
                        Cars = result.Cars,
                        SuggestedFilters = result.SuggestedFilters
                    };
                }
                case "recommendation":
                {
                    var result = await _toolLayerService.ExecuteAsync(
                        "recommend_cars",
                        JsonSerializer.Serialize(new
                        {
                            city = ExtractCity(userMessage),
                            preference = userMessage,
                            fuel_type = ExtractFuelType(userMessage),
                            max_price = ExtractMaxPrice(userMessage)
                        }),
                        cancellationToken);

                    return new AiCompletionResult
                    {
                        Message = "Tercihine gore en uygun araclari siraladim.",
                        Cars = result.Cars
                    };
                }
                case "faq":
                {
                    var result = await _toolLayerService.ExecuteAsync(
                        "faq_search",
                        JsonSerializer.Serialize(new { question = userMessage }),
                        cancellationToken);

                    return new AiCompletionResult
                    {
                        Message = JsonDocument.Parse(result.ResultJson).RootElement.TryGetProperty("answer", out var answerNode)
                            ? answerNode.GetString() ?? "Bu konuda yardimci olayim."
                            : "Bu konuda yardimci olayim."
                    };
                }
                default:
                    return new AiCompletionResult
                    {
                        Message = "AI baglantisi su an yok. Arac arama, fiyat veya rezervasyon sorunu daha net yazarsan lokal yardimci olabilirim."
                    };
            }
        }

        private static string? ExtractCity(string message)
        {
            var cities = new[] { "istanbul", "ankara", "izmir", "bursa", "antalya", "adana" };
            var text = message.ToLowerInvariant();
            return cities.FirstOrDefault(text.Contains);
        }

        private static string? ExtractVehicleType(string message)
        {
            var text = message.ToLowerInvariant();
            if (text.Contains("suv")) return "SUV";
            if (text.Contains("sedan")) return "Sedan";
            if (text.Contains("hatch")) return "Hatchback";
            if (text.Contains("minivan")) return "Minivan";
            return null;
        }

        private static string? ExtractFuelType(string message)
        {
            var text = message.ToLowerInvariant();
            if (text.Contains("dizel")) return "Dizel";
            if (text.Contains("hibrit")) return "Hibrit";
            if (text.Contains("elektrik")) return "Elektrik";
            if (text.Contains("benzin")) return "Benzin";
            return null;
        }

        private static decimal? ExtractMaxPrice(string message)
            => ExtractPriceByKeyword(message, "max", "en fazla", "bütçe", "butce", "ust");

        private static decimal? ExtractMinPrice(string message)
            => ExtractPriceByKeyword(message, "min", "en az", "alt");

        private static decimal? ExtractPriceByKeyword(string message, params string[] markers)
        {
            var digits = new string(message.Where(c => char.IsDigit(c) || c == ' ').ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(digits)) return null;

            var parts = digits.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts.Reverse())
            {
                if (decimal.TryParse(part, out var v))
                    return v;
            }

            return null;
        }

        private class OpenAiResponse
        {
            public string Content { get; set; } = string.Empty;
            public List<OpenAiToolCall> ToolCalls { get; set; } = new();
        }
    }
}
