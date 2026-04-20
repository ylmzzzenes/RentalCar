using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.AI.Configuration;
using RentalCar.Domain.Extensions;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services.Cars;

/// <summary>
/// Favoriler, kiralama / satın alma geçmişi ve ilan verisine göre LLM ile satın alma & kiralama önerisi.
/// </summary>
public sealed class CarListingInsightService : ICarListingInsightService
{
    private readonly RentalCarContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;
    private readonly ILogger<CarListingInsightService> _logger;

    public CarListingInsightService(
        RentalCarContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<CarListingInsightService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CarListingInsightResult> GetInsightsAsync(int carId, string? userId, CancellationToken cancellationToken = default)
    {
        var car = await _db.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == carId, cancellationToken);
        if (car == null)
        {
            return new CarListingInsightResult
            {
                SummaryText = "Araç bulunamadı.",
                PurchaseVerdict = "Bu ilan bulunamadığı için değerlendirme yapılamıyor.",
                PriceAnalysis = "-",
                RecommendationRationale = "-",
                Bullets = Array.Empty<string>(),
                Alternatives = Array.Empty<string>()
            };
        }

        var userContext = await BuildUserContextAsync(userId, carId, car, cancellationToken);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return FallbackInsights(car, userContext);
        }

        try
        {
            var json = await CallLlmAsync(car, userContext, cancellationToken);
            if (json is null)
                return FallbackInsights(car, userContext);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var ozet = root.TryGetProperty("ozet", out var o) ? o.GetString() ?? string.Empty : string.Empty;
            var bullets = ReadStringArray(root, "maddeler");
            var alts = ReadStringArray(root, "alternatifler");
            if (bullets.Count == 0 && !string.IsNullOrWhiteSpace(ozet))
                bullets = new List<string> { ozet };

            var alinir = ReadOptionalString(root, "alinir_mi");
            var fiyatDeger = ReadOptionalString(root, "fiyat_degerlendirme");
            var neden = ReadOptionalString(root, "neden");
            if (string.IsNullOrWhiteSpace(alinir))
                alinir = string.IsNullOrWhiteSpace(ozet) ? "Özet üretildi; detaylar maddelerde." : ozet;
            if (string.IsNullOrWhiteSpace(fiyatDeger))
            {
                fiyatDeger = bullets.FirstOrDefault(b =>
                    b.Contains("fiyat", StringComparison.OrdinalIgnoreCase) ||
                    b.Contains("piyasa", StringComparison.OrdinalIgnoreCase) ||
                    b.Contains("pahalı", StringComparison.OrdinalIgnoreCase) ||
                    b.Contains("uygun", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(fiyatDeger))
                fiyatDeger = "Fiyat yorumu için maddelere bakın.";
            if (string.IsNullOrWhiteSpace(neden))
                neden = "İlan verisi, tahmini fiyat ve (varsa) profiliniz modele iletildi.";

            return new CarListingInsightResult
            {
                UsedLlm = true,
                SummaryText = string.IsNullOrWhiteSpace(ozet) ? "Öneriler üretildi." : ozet,
                PurchaseVerdict = alinir,
                PriceAnalysis = fiyatDeger,
                RecommendationRationale = neden,
                Bullets = bullets,
                Alternatives = alts
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM öneri üretilemedi, fallback kullanılıyor.");
            return FallbackInsights(car, userContext);
        }
    }

    private async Task<string> BuildUserContextAsync(string? userId, int carId, Car car, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Kullanıcı oturumu yok; sadece ilan verisi kullanılacak.";

        var favCount = await _db.Favorites.AsNoTracking().CountAsync(f => f.UserId == userId, ct);
        var favBrands = await _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(_db.Cars.AsNoTracking(), f => f.CarId, c => c.Id, (_, c) => c.CatalogBrand ?? c.Brand)
            .Where(b => b != null)
            .Select(b => b!)
            .Distinct()
            .Take(8)
            .ToListAsync(ct);

        var rentalCount = await _db.Rentals.AsNoTracking().CountAsync(r => r.UserId == userId, ct);
        var purchaseCount = await _db.Purchases.AsNoTracking().CountAsync(p => p.UserId == userId, ct);

        var minP = car.ListedPrice.HasValue ? car.ListedPrice.Value * 0.85m : 0;
        var maxP = car.ListedPrice.HasValue ? car.ListedPrice.Value * 1.15m : 0;

        return $"""
            favori_sayisi: {favCount}
            favori_markalar: {string.Join(", ", favBrands)}
            gecmis_kiralama: {rentalCount}
            gecmis_satin_alma: {purchaseCount}
            fiyat_araligi_tl: {minP:N0} - {maxP:N0} (ilan fiyatına göre ±%15)
            """;
    }

    private async Task<string?> CallLlmAsync(Car car, string userContext, CancellationToken cancellationToken)
    {
        const string jsonSchema =
            """{"ozet":"tek cümle genel görüş","alinir_mi":"bu araç alınır mı — kısa net cevap","fiyat_degerlendirme":"pahalı mı uygun mu — 1-2 cümle","neden":"bu önerinin gerekçesi","maddeler":["madde1","madde2","madde3"],"alternatifler":["alternatif1","alternatif2"]}""";

        var prompt = $"""
            Sen Türkiye ikinci el araç pazarı uzmanısın. Kullanıcıya hem SATIN ALMA hem KİRALAMA kararı için kısa, güvenilir öneri ver.
            Yanıtını SADECE geçerli JSON olarak ver (başka metin yok). Şema:
            {jsonSchema}

            İlan:
            - marka: {car.CatalogBrand ?? car.Brand}
            - seri: {car.Series}
            - model: {car.Model}
            - yıl: {car.ModelYear}
            - km: {car.OdometerKm}
            - ilan_fiyat_tl: {car.ListedPrice}
            - tahmini_ai_fiyat: {car.PredictedPriceMid}
            - yakıt: {car.FuelType.GetDisplayName()}
            - vites: {car.Transmission.GetDisplayName()}
            - kasa: {car.BodyType.GetDisplayName()}
            - durum: {car.VehicleCondition.GetDisplayName()}
            - kimden: {car.SellerType.GetDisplayName()}

            Kullanıcı profili:
            {userContext}

            Kurallar:
            - "alinir_mi" ve "fiyat_degerlendirme" alanlarını mutlaka doldur.
            - "neden" alanında hangi sinyallere dayandığını (fiyat, km, yaş, yakıt, satıcı tipi) kısaca yaz.
            - Fiyatı ilan ve tahminle karşılaştır; çok yüksek/düşükse belirt.
            - Kiralama için: günlük fiyat yoksa ilan fiyatından türetilmiş günlük değeri kabaca yorumla.
            - Alternatifler: benzer segmentte 1-2 mantıklı seçenek tarzı (model önerisi, somut başka ilan yok).
            - Türkçe, net, abartısız.
            """;

        var payload = new
        {
            model = _options.Model,
            temperature = 0.35,
            messages = new object[]
            {
                new { role = "system", content = "Yalnızca geçerli JSON döndür. Markdown kullanma." },
                new { role = "user", content = prompt }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var text = content.Trim();
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;
        return text[start..(end + 1)];
    }

    private static string ReadOptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return string.Empty;
        return (el.GetString() ?? string.Empty).Trim();
    }

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new List<string>();

        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                list.Add(s.Trim());
        }
        return list;
    }

    private static CarListingInsightResult FallbackInsights(Car car, string userContext)
    {
        var bullets = new List<string>();
        var listed = car.ListedPrice ?? 0;
        var mid = car.PredictedPriceMid ?? 0;
        var priceAnalysis = "Fiyat karşılaştırması için yeterli veri yok; benzer ilanları inceleyin.";
        var purchase = "Karar için ekspertiz, hasar/km tutarlılığı ve resmi kayıt sorgusu önerilir.";

        if (listed > 0 && mid > 0)
        {
            var ratio = listed / mid;
            if (ratio > 1.12m)
            {
                priceAnalysis = "İlan fiyatı tahmini piyasa değerinin üzerinde; pazarlık ve ekspertiz mantıklı.";
                purchase = "Fiyat üst bantta; araç geçmişi temizse pazarlıkla değerlendirilebilir, aksi halde beklemek daha güvenli olabilir.";
                bullets.Add("İlan fiyatı, tahmini piyasa değerinin üzerinde görünüyor; pazarlık ve ekspertiz önerilir.");
            }
            else if (ratio < 0.88m)
            {
                priceAnalysis = "İlan fiyatı tahminin altında; detayları (hasar, km, kayıt) mutlaka doğrulayın.";
                purchase = "Fiyat cazip görünüyor; düşük fiyatın gerekçesini ekspertiz ve sorgu ile netleştirin.";
                bullets.Add("İlan fiyatı tahminin altında; kayıt / hasar / km detaylarını mutlaka doğrulayın.");
            }
            else
            {
                priceAnalysis = "Fiyat tahmini aralığa yakın; genel olarak dengeli bir etiket gibi duruyor.";
                purchase = "Fiyat bandı makul; son karar için araç durumu ve ekspertiz belirleyici olacaktır.";
                bullets.Add("Fiyat, tahmini aralığa yakın; karar için araç geçmişi ve ekspertiz önemli.");
            }
        }
        else
            bullets.Add("Fiyat karşılaştırması için yeterli veri yok; benzer ilanları inceleyin.");

        bullets.Add(car.DailyPrice > 0
            ? $"Kiralama için günlük taban fiyat yaklaşık {car.DailyPrice:N0} TL (ilan üzerinden hesaplanmış olabilir)."
            : "Kiralama fiyatı tanımlı değilse günlük ücret ilan fiyatından türetilmiş olabilir; net bilgi için ilan sahibine sorun.");

        var rationale = string.IsNullOrWhiteSpace(userContext) || userContext.Contains("oturumu yok", StringComparison.OrdinalIgnoreCase)
            ? "Kural tabanlı özet: ilan fiyatı, tahmini değer ve kiralama alanları kullanıldı. Giriş yaparak profil sinyalleri eklenebilir."
            : "Kural tabanlı özet: ilan verisi ile birlikte favori ve geçmiş özetiniz modele iletilirdi (API kapalıysa bu metin kullanılır).";

        bullets.Add(string.IsNullOrWhiteSpace(userContext) || userContext.Contains("oturumu yok", StringComparison.OrdinalIgnoreCase)
            ? "Giriş yaparak favori ve geçmişinize göre kişiselleştirilmiş öneri alabilirsiniz."
            : "Geçmiş işlemleriniz ve favorileriniz LLM ile birlikte değerlendirildi (API kapalıysa kural tabanlı özet gösterilir).");

        return new CarListingInsightResult
        {
            UsedLlm = false,
            SummaryText = "API anahtarı yok veya servis yanıt vermedi; kural tabanlı özet gösteriliyor.",
            PurchaseVerdict = purchase,
            PriceAnalysis = priceAnalysis,
            RecommendationRationale = rationale,
            Bullets = bullets,
            Alternatives = new List<string>
            {
                "Aynı segmentte bir üst/alt kasa tipiyle karşılaştırın.",
                "Benzer km ve yılda 2-3 ilan daha inceleyip fiyat dağılımına bakın."
            }
        };
    }
}
