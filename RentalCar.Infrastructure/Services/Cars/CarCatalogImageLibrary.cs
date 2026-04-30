namespace RentalCar.Infrastructure.Services.Cars;

/// <summary>
/// Manuel marka-model → uzak görsel URL eşlemesi. LLM kullanılmaz.
/// URL'ler Wikimedia Commons üzerinde doğrulanmış <b>330px küçük önizleme</b> adresleridir (tam boyut 429/404 riskini azaltır).
/// </summary>
public static class CarCatalogImageLibrary
{
    public static string NormalizeKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().ToLowerInvariant();
        normalized = normalized
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c");

        var chars = normalized.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-').ToArray();
        normalized = new string(chars).Trim();
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        return normalized;
    }

    /// <summary>C 200 → c200 gibi eşleşmeler için boşluksuz anahtar.</summary>
    public static string CompactKey(string normalized)
    {
        return normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    public static string BuildLookupKey(string? catalogBrand, string? brand, string? catalogModel, string? model)
    {
        var b = NormalizeKey(catalogBrand ?? brand);
        var m = NormalizeKey(catalogModel ?? model);
        if (string.IsNullOrEmpty(b) || string.IsNullOrEmpty(m))
            return string.Empty;
        return $"{b}|{m}";
    }

    public static IReadOnlyList<string>? TryGetRemoteUrls(string lookupKey)
    {
        if (string.IsNullOrEmpty(lookupKey))
            return null;

        if (ManualImageCatalog.TryGetValue(lookupKey, out var urls))
            return urls;

        var pipe = lookupKey.IndexOf('|', StringComparison.Ordinal);
        if (pipe > 0 && pipe < lookupKey.Length - 1)
        {
            var brand = lookupKey[..pipe];
            var model = lookupKey[(pipe + 1)..];
            var compact = $"{brand}|{CompactKey(model)}";
            if (ManualImageCatalog.TryGetValue(compact, out var urls2))
                return urls2;
        }

        return null;
    }

    /// <summary>Veritabanı / rapor için: sözlükte tanımlı tüm marka|model anahtarları.</summary>
    public static IReadOnlyCollection<string> AllCatalogKeys => ManualImageCatalog.Keys;

    private static readonly Dictionary<string, string[]> ManualImageCatalog = new(StringComparer.Ordinal)
    {
        ["bmw|320i"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/df/BMW_G20_320i_Black_Sapphire_Metallic_%282%29.jpg/330px-BMW_G20_320i_Black_Sapphire_Metallic_%282%29.jpg"
        ],
        ["bmw|520i"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4c/BMW_G30_520d_M_Sport_Alpine_White_%281%29.jpg/330px-BMW_G30_520d_M_Sport_Alpine_White_%281%29.jpg"
        ],
        ["mercedes-benz|c 200"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg/330px-Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg"
        ],
        ["mercedes|c 200"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg/330px-Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg"
        ],
        ["mercedes|c200"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg/330px-Mercedes-Benz_C200_4MATIC_AVANTGARDE_%28W205%29_front.jpg"
        ],
        ["audi|a4"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/b/ba/Audi_A4_B9_sedans_%28FL%29_1X7A6817.jpg/330px-Audi_A4_B9_sedans_%28FL%29_1X7A6817.jpg"
        ],
        ["audi|a6"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/6/68/Audi_A6_Allroad_Quattro_C8_1X7A0302.jpg/330px-Audi_A6_Allroad_Quattro_C8_1X7A0302.jpg"
        ],
        ["volkswagen|passat"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/9/91/VW_Passat_B8_Limousine_2.0_TDI_Highline.JPG/330px-VW_Passat_B8_Limousine_2.0_TDI_Highline.JPG"
        ],
        ["volkswagen|golf"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ed/Volkswagen_Golf_VIII_R_20_Years_Auto_Zuerich_2023_1X7A1358.jpg/330px-Volkswagen_Golf_VIII_R_20_Years_Auto_Zuerich_2023_1X7A1358.jpg"
        ],
        ["toyota|corolla"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c5/Toyota_Corolla_Trek_Hybrid_Genf_2019_1Y7A5587.jpg/330px-Toyota_Corolla_Trek_Hybrid_Genf_2019_1Y7A5587.jpg"
        ],
        ["honda|civic"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/0/00/Honda_Civic_XI_IMG001.jpg/330px-Honda_Civic_XI_IMG001.jpg"
        ],
        ["renault|megane"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/0/04/Renault_Megane_IV_Sedan_1X7A0225.jpg/330px-Renault_Megane_IV_Sedan_1X7A0225.jpg"
        ],
        ["fiat|egea"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Fiat_Tipo_Sedan_Facelift_Leonberg_2022_1X7A0412.jpg/330px-Fiat_Tipo_Sedan_Facelift_Leonberg_2022_1X7A0412.jpg"
        ],
        ["dacia|lodgy"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4c/Dacia_Lodgy_glace_2.jpg/330px-Dacia_Lodgy_glace_2.jpg"
        ],
        ["ford|courier"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/5/5b/Ford_Transit_Courier_%282nd_generation%29_1X7A2516.jpg/330px-Ford_Transit_Courier_%282nd_generation%29_1X7A2516.jpg"
        ],
        ["ford|fiesta"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/7/7d/2017_Ford_Fiesta_Zetec_Turbo_1.0_Front.jpg/330px-2017_Ford_Fiesta_Zetec_Turbo_1.0_Front.jpg"
        ],
        ["ford|focus"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/9/96/2018_Ford_Focus_ST-Line_TDCi_1.5_Front.jpg/330px-2018_Ford_Focus_ST-Line_TDCi_1.5_Front.jpg"
        ],
        ["hyundai|i20"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2f/Hyundai_i20_N_IMG_5400.jpg/330px-Hyundai_i20_N_IMG_5400.jpg"
        ],
        ["peugeot|308"] =
        [
            "https://upload.wikimedia.org/wikipedia/commons/thumb/3/37/2020_-_Peugeot_308_II_%28B%29_-_55.jpg/330px-2020_-_Peugeot_308_II_%28B%29_-_55.jpg"
        ]
    };
}
