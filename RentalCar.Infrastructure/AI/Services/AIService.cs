using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AIService> _logger;

        public AIService(
            HttpClient httpClient,
            IOptions<OpenAiOptions> options,
            IIntentClassifier intentClassifier,
            IConversationMemoryService memoryService,
            IToolLayerService toolLayerService,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache,
            ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _intentClassifier = intentClassifier;
            _memoryService = memoryService;
            _toolLayerService = toolLayerService;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AiCompletionResult> ReplyAsync(string userMessage, string sessionId, CancellationToken cancellationToken = default)
        {
            var intent = _intentClassifier.Classify(userMessage);
            ValidateInput(userMessage);
            EnsureRateLimit();
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));

            await _memoryService.AppendAsync(sessionId, "user", userMessage, cancellationToken);

            var history = await _memoryService.GetMessagesAsync(sessionId, cancellationToken);
            var messageList = BuildMessages(history, userMessage);

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("AI fallback active because API key is missing. PromptVersion: {PromptVersion}", _options.PromptVersion);
                var fallback = await BuildToolOnlyFallbackAsync(intent, userMessage, cancellationToken);
                await _memoryService.AppendAsync(sessionId, "assistant", fallback.Message, cancellationToken);
                fallback.Intent = intent;
                return fallback;
            }

            var toolDefsJson = _toolLayerService.BuildToolDefinitionJson();
            OpenAiResponse firstPass;
            try
            {
                firstPass = await CreateChatCompletionWithRetryAsync(messageList, toolDefsJson, true, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("invalid_api_key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "Invalid Groq API key. Falling back to tool-only response.");
                var fallback = await BuildToolOnlyFallbackAsync(intent, userMessage, cancellationToken);
                await _memoryService.AppendAsync(sessionId, "assistant", fallback.Message, cancellationToken);
                fallback.Intent = intent;
                return fallback;
            }

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

                current = await CreateChatCompletionWithRetryAsync(messageList, toolDefsJson, true, cancellationToken);
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
                        "Sen RentalCar AI Assistant'sin. Bu platform arac listeleme, kiralama ve satis icin kullanilir. PromptVersion=" + _options.PromptVersion + ". " +
                        "Kisa, net ve guvenli cevap ver. " +
                        "Asla uydurma bilgi verme. Emin degilsen bunu acikca soyle. " +
                        "Arac arama, fiyatlama, satis degerlendirmesi, kiralama plani ve SSS sorularinda gerekiyorsa tool kullan. " +
                        "Tool sonucu olmadan kesin fiyat, uygunluk veya teknik iddia kurma. " +
                        "Yanitta markdown tablo kullanma. " +
                        "Kullanici sadece genel soru sorarsa once netlestirici ama tek cumlelik soru sor. " +
                        "Tool sonucu varsa oncelikle o sonuca dayan, olmayan alanlari tahmin etme. " +
                        "Arac ararken search_cars aracını kullan ve kullanıcının marka/model/seri soyledigi metni mutlaka `query` alanına yaz; sadece sehir veya kasa tipi ile sinirli kalma."
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

        private async Task<OpenAiResponse> CreateChatCompletionWithRetryAsync(
            JsonArray messages,
            string toolsJson,
            bool allowTools,
            CancellationToken cancellationToken)
        {
            var attempts = Math.Max(1, _options.RetryCount + 1);
            Exception? lastError = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    return await CreateChatCompletionAsync(messages, toolsJson, allowTools, cancellationToken);
                }
                catch (Exception ex) when (attempt < attempts)
                {
                    lastError = ex;
                    _logger.LogWarning(ex,
                        "AI completion attempt failed. Attempt: {Attempt}/{Attempts}, Model: {Model}, BaseUrl: {BaseUrl}",
                        attempt, attempts, _options.Model, _options.BaseUrl);
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), cancellationToken);
                }
            }

            _logger.LogError(lastError, "AI completion failed after retries. Model: {Model}, BaseUrl: {BaseUrl}", _options.Model, _options.BaseUrl);
            throw lastError ?? new InvalidOperationException("AI completion failed.");
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
                ["temperature"] = 0.1,
                ["messages"] = JsonNode.Parse(messages.ToJsonString())
            };

            if (allowTools)
            {
                payload["tools"] = JsonNode.Parse(toolsJson);
                payload["tool_choice"] = "auto";
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending AI completion request. Model: {Model}, PromptVersion: {PromptVersion}, AllowTools: {AllowTools}",
                _options.Model, _options.PromptVersion, allowTools);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"LLM istegi basarisiz: {(int)response.StatusCode} - {raw}");
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
                case "fallback":
                    return new AiCompletionResult
                    {
                        Message = "Merhaba! Hangi marka veya modeli arıyorsun? Örnek: \"İstanbul dizel SUV\" veya \"Toyota Corolla\".",
                        Intent = "fallback"
                    };
                case "booking_help":
                    return new AiCompletionResult
                    {
                        Message = "Kiralama için ilan kartından \"Kirala\"ya tıklayıp tarih ve süreyi seçebilirsin. Belirli bir araç için ilan numarasını veya marka/modeli yaz.",
                        Intent = "booking_help"
                    };
                case "car_search":
                {
                    var result = await _toolLayerService.ExecuteAsync(
                        "search_cars",
                        JsonSerializer.Serialize(new
                        {
                            query = userMessage,
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
                case "pricing":
                    return new AiCompletionResult
                    {
                        Message = "Fiyat hesabi icin soyle yazabilirsin: \"3 gunluk X araci kac tl\" veya ilan sayfasindan Kirala akisini kullan. AI baglantisi yokken net tutar veremiyorum."
                    };
                default:
                {
                    var result = await _toolLayerService.ExecuteAsync(
                        "search_cars",
                        JsonSerializer.Serialize(new
                        {
                            query = userMessage,
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
                            ? $"{result.Cars.Count} ilan listeliyorum. Daha net sonuc icin marka veya model de yazabilirsin."
                            : "Eslesen ilan yok; marka/model veya butceyi farklilastirmayi dene.",
                        Cars = result.Cars,
                        SuggestedFilters = result.SuggestedFilters
                    };
                }
            }
        }

        private static string? ExtractCity(string message)
        {
            var cities = new[]
            {
                "istanbul", "ankara", "izmir", "bursa", "antalya", "adana", "konya", "gaziantep",
                "kayseri", "mersin", "eskisehir", "diyarbakir", "samsun", "denizli", "sakarya",
                "malatya", "kahramanmaras", "erzurum", "van", "batman", "elazig", "tekirdag"
            };
            var text = message.ToLowerInvariant().Replace('ı', 'i');
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
            if (text.Contains("lpg")) return "Benzin";
            if (text.Contains("benzin")) return "Benzin";
            return null;
        }

        private static decimal? ExtractMaxPrice(string message)
        {
            if (!HasMoneyContext(message)) return null;
            var lower = message.ToLowerInvariant();
            var wantsMax = Regex.IsMatch(lower, @"en\s*fazla|maks|max\b|altında|altinda|bütçe|butce|üst|ust|kadar|geçmez|gecmez");
            var wantsMin = Regex.IsMatch(lower, @"en\s*az|minimum|min\b");
            var amount = ExtractLargestMoneyAmount(message);
            if (!amount.HasValue) return null;
            if (wantsMin && !wantsMax) return null;
            if (wantsMax) return amount;
            if (!wantsMin) return amount;
            return null;
        }

        private static decimal? ExtractMinPrice(string message)
        {
            if (!HasMoneyContext(message)) return null;
            var lower = message.ToLowerInvariant();
            if (!Regex.IsMatch(lower, @"en\s*az|minimum|min\b")) return null;
            return ExtractLargestMoneyAmount(message);
        }

        private static bool HasMoneyContext(string message)
        {
            var lower = message.ToLowerInvariant();
            return lower.Contains("tl") || lower.Contains("lira") || lower.Contains("bin") || lower.Contains("milyon")
                || lower.Contains("bütçe") || lower.Contains("butce");
        }

        private static decimal? ExtractLargestMoneyAmount(string message)
        {
            decimal? best = null;
            foreach (Match m in Regex.Matches(message, @"(?<num>\d{1,3}(?:[.\s]\d{3})+|\d+)\s*(?<mul>bin|milyon|mi)?", RegexOptions.IgnoreCase))
            {
                var raw = m.Groups["num"].Value.Replace(".", "").Replace(" ", "");
                if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var n))
                    continue;
                var mul = m.Groups["mul"].Value.ToLowerInvariant();
                if (mul is "bin") n *= 1000;
                if (mul.Contains("milyon") || mul == "mi") n *= 1_000_000;
                if (!best.HasValue || n > best.Value) best = n;
            }

            return best;
        }

        private class OpenAiResponse
        {
            public string Content { get; set; } = string.Empty;
            public List<OpenAiToolCall> ToolCalls { get; set; } = new();
        }
    }
}
