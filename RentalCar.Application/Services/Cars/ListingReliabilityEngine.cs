using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;

namespace RentalCar.Application.Services.Cars;

/// <summary>
/// Ağırlıklı alt skorlar (0–100) ile güvenilirlik; kritik şüphelerde ek cezalar.
/// </summary>
public sealed class ListingReliabilityEngine(ILogger<ListingReliabilityEngine> logger)
{
    private static readonly Regex DamageMentionPattern = new(
        @"boya|değişen|değisen|kaporta|çizik|ezik|lokal|boyalı|onarım|çarpma|hasar|göçük|dolu|yedek\s*parça",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    private const double WDescription = 0.16;
    private const double WConsistency = 0.15;
    private const double WPrice = 0.22;
    private const double WSeller = 0.12;
    private const double WImages = 0.13;
    private const double WDuplicate = 0.08;
    /// <summary>Şehir, teknik alanlar, geçmiş bilgisi — ilanlar arası doğal ayrışma.</summary>
    private const double WDetailRichness = 0.14;

    public ListingReliabilityResult Compute(Car car, ListingReliabilitySignals signals)
    {
        var desc = ScoreDescription(car);
        var consistency = ScoreConsistency(car);
        var price = ScorePrice(car, signals);
        var seller = ScoreSeller(car, signals);
        var images = ScoreImages(car);
        var dup = ScoreDuplicates(signals);
        var detail = ScoreDetailRichness(car);

        var factors = new List<ListingReliabilityFactorDetail>
        {
            Factor("description", "Açıklama kalitesi", WDescription, desc.Score, desc.Note),
            Factor("consistency", "Tutarlılık (form / metin)", WConsistency, consistency.Score, consistency.Note),
            Factor("price", "Fiyat / piyasa", WPrice, price.Score, price.Note),
            Factor("seller", "Satıcı geçmişi", WSeller, seller.Score, seller.Note),
            Factor("images", "Görseller", WImages, images.Score, images.Note),
            Factor("duplicate", "Benzer / tekrar ilan", WDuplicate, dup.Score, dup.Note),
            Factor("detail", "İlan detay zenginliği", WDetailRichness, detail.Score, detail.Note)
        };

        var weightSum = WDescription + WConsistency + WPrice + WSeller + WImages + WDuplicate + WDetailRichness;
        var blended = factors.Sum(f => f.WeightedContribution) / weightSum;

        var adjustment = ApplySuspicionAdjustments(car, signals, desc.Score, consistency.Score, price.Score, dup.Score, factors);
        var raw = blended + adjustment;

        var completenessPenalty = CompletenessPenalty(car);
        raw -= completenessPenalty;

        var score = (int)Math.Round(Math.Clamp(raw, 0, 100));
        var (label, trustTr) = MapLabel(score);
        var explanation = BuildUserExplanation(
            desc.Score, consistency.Score, price.Score, seller.Score, images.Score, dup.Score, detail.Score, car, signals);

        logger.LogInformation(
            "ListingReliability CarId={CarId} Score={Score} Blend={Blend:F2} Adj={Adj:F2} CompletePen={Cp:F2} " +
            "Desc={Desc:F1} Cons={Cons:F1} Price={Price:F1} Seller={Seller:F1} Img={Img:F1} Dup={Dup:F1} Detail={Detail:F1} Similar={Sim} SellerListings={Sl} PeerN={Pn}",
            car.Id,
            score,
            blended,
            adjustment,
            completenessPenalty,
            desc.Score,
            consistency.Score,
            price.Score,
            seller.Score,
            images.Score,
            dup.Score,
            detail.Score,
            signals.SimilarListingCount,
            signals.SellerListingCount,
            signals.PeerPriceSampleSize);

        return new ListingReliabilityResult
        {
            Score = score,
            Label = label,
            TrustLevelTr = trustTr,
            UserExplanation = explanation,
            Factors = factors
        };
    }

    private static ListingReliabilityFactorDetail Factor(string code, string name, double w, double sub, string? note) =>
        new()
        {
            Code = code,
            DisplayName = name,
            Weight = w,
            Subscore = Math.Round(sub, 2),
            WeightedContribution = Math.Round(w * sub, 4),
            Note = note
        };

    private static (double Score, string? Note) ScoreDescription(Car car)
    {
        var text = $"{car.ShortDescription} {car.FullDescription}".Trim();
        if (string.IsNullOrWhiteSpace(text))
            return (28, "Açıklama yok.");

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var wc = words.Length;
        var len = text.Length;
        var distinct = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var uniqueRatio = wc > 0 ? (double)distinct / wc : 0;

        double s = 38;
        if (wc >= 8) s += 12;
        if (wc >= 22) s += 14;
        if (wc >= 45) s += 12;
        if (len >= 280) s += 10;
        if (Regex.IsMatch(text, @"\d{3,}", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            s += 6;
        if (uniqueRatio < 0.32 && wc > 18)
            s -= 22;

        s = Math.Clamp(s, 0, 100);

        string? note = len < 80 ? "Kısa metin." :
            wc < 12 ? "Az kelime." :
            uniqueRatio < 0.35 && wc > 18 ? "Tekrarlı / düşük çeşitlilik." : null;

        return (s, note);
    }

    private static (double Score, string? Note) ScoreConsistency(Car car)
    {
        var desc = $"{car.ShortDescription} {car.FullDescription}";
        var mentionsDamage = !string.IsNullOrWhiteSpace(desc) && DamageMentionPattern.IsMatch(desc);
        var bodyEmpty = string.IsNullOrWhiteSpace(car.BodyWorkNotes);

        if (mentionsDamage && bodyEmpty)
            return (22, "Açıklamada boya/değişen vb. var; alan boş.");

        double s = 82;
        if (car.VehicleCondition == VehicleCondition.Belirtilmedi)
            s -= 10;
        if (car.SellerType == ListingSellerType.Belirtilmedi)
            s -= 6;
        if (car.HasAccidentRecord is null && mentionsDamage)
            s -= 8;

        if (!mentionsDamage && !bodyEmpty)
            s = Math.Min(s, 72);

        s = Math.Clamp(s, 0, 100);
        return (s, s < 55 ? "Form ile metin zayıf uyum." : null);
    }

    private static (double Score, string? Note) ScorePrice(Car car, ListingReliabilitySignals sig)
    {
        var listed = car.ListedPrice;
        if (listed is null or <= 0)
            return (18, "Liste fiyatı yok.");

        var mid = car.PredictedPriceMid;
        decimal? benchmark = mid is > 0 ? mid : sig.PeerAverageListedPrice;
        if (benchmark is null or <= 0)
            return (58, "Piyasa bandı yok; nötr.");

        var ratio = (double)(listed.Value / benchmark.Value);
        if (ratio <= 0 || double.IsNaN(ratio) || double.IsInfinity(ratio))
            return (20, "Fiyat oranı hesaplanamadı.");

        // Sürekli eğri: küçük fiyat farkları bile skoru ayırır (geniş platö yok).
        var logRatio = Math.Log(ratio);
        var bell = Math.Exp(-3.6 * logRatio * logRatio);
        var s = 18 + 82 * bell;

        if (ratio < 0.42)
            s = Math.Min(s, 34);
        else if (ratio < 0.55)
            s = Math.Min(s, 48);
        if (ratio > 1.85)
            s = Math.Min(s, 36);
        else if (ratio > 1.45)
            s = Math.Min(s, 52);

        string? note = null;
        if (ratio < 0.55)
            note = "Fiyat tahmine / segmente göre düşük.";
        else if (ratio > 1.42)
            note = "Fiyat tahmine / segmente göre yüksek.";
        else if (bell < 0.88)
            note = "Fiyat piyasa bandından uzak.";

        if (mid is > 0 && sig.PeerAverageListedPrice is > 0 && sig.PeerPriceSampleSize >= 3)
        {
            var peerRatio = (double)(sig.PeerAverageListedPrice.Value / mid.Value);
            if (peerRatio < 0.75 || peerRatio > 1.35)
                s -= 5;
        }

        return (Math.Clamp(s, 0, 100), note);
    }

    private static (double Score, string? Note) ScoreSeller(Car car, ListingReliabilitySignals sig)
    {
        if (string.IsNullOrWhiteSpace(car.PostedByUserId))
            return (52, "Satıcı hesabı yok / anonim.");

        var n = Math.Max(sig.SellerListingCount, 1);
        double volume;
        if (n <= 1)
            volume = 48;
        else if (n <= 2)
            volume = 56;
        else if (n <= 5)
            volume = 66;
        else if (n <= 12)
            volume = 74;
        else
            volume = 82;

        if (sig.SellerRatingSampleSize <= 0)
            return (Math.Clamp(volume, 0, 100), n <= 1 ? "İlk ilanları olabilir." : null);

        var r = sig.SellerAverageCarRating ?? 3;
        var ratingPart = Math.Clamp(r / 5.0 * 100, 0, 100);
        var blended = 0.55 * volume + 0.45 * ratingPart;

        var note = sig.SellerRatingSampleSize < 3 ? "Az değerlendirme örneği." : null;
        return (Math.Clamp(blended, 0, 100), note);
    }

    private static (double Score, string? Note) ScoreImages(Car car)
    {
        var urls = car.ImageUrls?
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToList() ?? new List<string>();

        bool placeholder(string u) =>
            u.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("via.placeholder", StringComparison.OrdinalIgnoreCase);

        var real = urls.Where(u => !placeholder(u)).ToList();
        var n = real.Count;

        if (n == 0)
        {
            var onlyPh = urls.Count > 0;
            return (onlyPh ? 26 : 30, onlyPh ? "Yalnızca yer tutucu görsel." : "Görsel yok.");
        }

        var s = 44 + 11.2 * n;
        s = Math.Min(94, s);
        if (n >= 6)
            s = Math.Min(94, s + 2);

        return (Math.Clamp(s, 0, 100), n < 3 ? "Az fotoğraf." : null);
    }

    /// <summary>Doldurulmuş alan sayısı ve çeşitliliği — aynı modele sahip ilanları ayırır.</summary>
    private static (double Score, string? Note) ScoreDetailRichness(Car car)
    {
        var bits = 0;
        const int maxBits = 16;

        void One(bool ok)
        {
            if (ok) bits++;
        }

        One(!string.IsNullOrWhiteSpace(car.City));
        One(!string.IsNullOrWhiteSpace(car.Color));
        One(!string.IsNullOrWhiteSpace(car.Series));
        One(!string.IsNullOrWhiteSpace(car.TrimPackage));
        One(!string.IsNullOrWhiteSpace(car.EngineCode));
        One(car.EnginePowerHp is > 0);
        One(car.EngineDisplacementLiters is > 0);
        One(car.FuelConsumptionLPer100Km is double d && d > 0);
        One(car.TorqueNm is > 0);
        One(car.FuelTankLiters is > 0);
        One(car.HasAccidentRecord.HasValue);
        One(car.HasServiceHistory.HasValue);
        One(car.PreviousOwnerCount.HasValue);
        One(!string.IsNullOrWhiteSpace(car.BodyWorkNotes));
        One(!string.IsNullOrWhiteSpace(car.Vin));
        One(!string.IsNullOrWhiteSpace(car.Plate));

        var ratio = (double)bits / maxBits;
        var s = 32 + 64 * ratio;
        if (car.BodyType != BodyType.None)
            s += 4;
        s = Math.Clamp(s, 0, 100);

        var note = bits <= 4 ? "Çok az teknik / konum detayı." :
            bits <= 8 ? "Bazı detaylar eksik." : null;

        return (s, note);
    }

    private static (double Score, string? Note) ScoreDuplicates(ListingReliabilitySignals sig)
    {
        var c = sig.SimilarListingCount;
        double s = c switch
        {
            0 => 96,
            1 => 58,
            2 => 38,
            _ => 22
        };
        string? note = c > 0 ? $"{c} benzer ilan." : null;
        return (s, note);
    }

    private static double ApplySuspicionAdjustments(
        Car car,
        ListingReliabilitySignals sig,
        double desc,
        double consistency,
        double price,
        double dup,
        List<ListingReliabilityFactorDetail> factors)
    {
        double adj = 0;

        if (price < 42 && dup < 72)
            adj -= 9;

        if (consistency < 42)
            adj -= 11;

        if (sig.SimilarListingCount >= 2 && price < 50)
            adj -= 7;

        if (car.ListedPrice is > 0 && car.PredictedPriceMid is > 0)
        {
            var r = (double)(car.ListedPrice.Value / car.PredictedPriceMid.Value);
            if (r < 0.55)
                adj -= 8;
        }

        if (adj < -0.001)
        {
            factors.Add(new ListingReliabilityFactorDetail
            {
                Code = "suspicion",
                DisplayName = "Şüphe düzeltmesi",
                Weight = 0,
                Subscore = 0,
                WeightedContribution = 0,
                Note = $"Toplam ceza: {adj:F1} puan."
            });
        }

        return adj;
    }

    private static double CompletenessPenalty(Car car)
    {
        double p = 0;
        if (string.IsNullOrWhiteSpace(car.CatalogBrand ?? car.Brand))
            p += 14;
        if (string.IsNullOrWhiteSpace(car.CatalogModelName ?? car.Model))
            p += 14;
        if (car.ListedPrice is null or <= 0)
            p += 18;
        if (!car.ModelYear.HasValue || car.ModelYear < 1980 || car.ModelYear > DateTime.UtcNow.Year + 1)
            p += 10;
        if (!car.OdometerKm.HasValue || car.OdometerKm < 0 || car.OdometerKm > 750_000)
            p += 8;

        return p;
    }

    private static (string Label, string TrustTr) MapLabel(int score) => score switch
    {
        >= 70 => ("High", "Yüksek güven"),
        >= 50 => ("Medium", "Orta güven"),
        _ => ("Low", "Düşük güven")
    };

    private static string BuildUserExplanation(
        double desc,
        double consistency,
        double price,
        double seller,
        double images,
        double dup,
        double detail,
        Car car,
        ListingReliabilitySignals sig)
    {
        var parts = new List<string>();
        if (desc < 52)
            parts.Add("açıklama kısa veya yüzeysel");
        if (consistency < 55)
            parts.Add("formdaki bilgiler ile metin uyumsuz olabilir");
        if (price < 52)
            parts.Add("fiyat piyasaya göre uç veya belirsiz");
        if (seller < 52)
            parts.Add("satıcı geçmişi veya değerlendirmeler sınırlı");
        if (images < 55)
            parts.Add("görsel seti zayıf");
        if (dup < 62)
            parts.Add("benzer başka ilanlar var");
        if (detail < 50)
            parts.Add("ilan formunda az sayıda ek teknik/konum bilgisi var");

        if (sig.SimilarListingCount >= 2)
            parts.Add("tekrar eden ilan riski");

        if (parts.Count == 0)
        {
            if (car.PredictedPriceMid is > 0 && car.ListedPrice is > 0)
                return "Bilgiler ve fiyat bandı genel olarak tutarlı görünüyor; yine de yerinde inceleme önerilir.";
            return "Temel alanlar dolu; karşılaştırma için detay ve görsellere bakmanızı öneririz.";
        }

        var tail = string.Join(", ", parts.Distinct().Take(3));
        return char.ToUpperInvariant(tail[0]) + tail[1..] + ".";
    }
}
