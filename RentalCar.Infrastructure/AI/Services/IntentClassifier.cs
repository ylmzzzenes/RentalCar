using RentalCar.Application.Abstractions.AI;

namespace RentalCar.AI.Services
{
    public class IntentClassifier : IIntentClassifier
    {
        public string Classify(string message)
        {
            var normalized = (message ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return "fallback";

            if (IsSmallTalk(normalized)) return "fallback";

            if (ContainsAny(normalized, "sss", "iade", "iptal", "sigorta", "ehliyet", "depozito", "hasar kaydi"))
                return "faq";

            if (ContainsAny(normalized, "rezerv", "book"))
                return "booking_help";

            if (ContainsAny(normalized, "oner", "tavsiye", "en mantikli", "bana ne alayim", "ne alayim"))
                return "recommendation";

            if (IsPricingIntent(normalized))
                return "pricing";

            if (IsCarSearchIntent(normalized))
                return "car_search";

            return "car_search";
        }

        private static bool IsSmallTalk(string n)
            => n.Length <= 12 && ContainsAny(n, "merhaba", "selam", "slm", "hey", "hi", "hello", "yardim", "yardım", "nasilsin", "nasılsın", "tesekkur", "teşekkür", "sagol", "sağol");

        private static bool IsPricingIntent(string n)
        {
            if (ContainsAny(n, "hesapla", "kaç tl", "kac tl", "ne kadar tutar", "toplam tutar", "gunluk ucret", "günlük ücret", "haftalik fiyat", "aylik fiyat"))
                return true;
            if (ContainsAny(n, "depozito", "maliyet", "ucret hesap", "ücret hesap"))
                return true;
            return false;
        }

        private static bool IsCarSearchIntent(string n)
        {
            if (ContainsAny(n, "bul", "ara", "listele", "goster", "göster", "var mi", "varmı", "arac", "araba", "otomobil", "ilan", "suv", "sedan", "hatch", "pickup", "minivan"))
                return true;
            if (ContainsAny(n, "dizel", "benzin", "hibrit", "elektrik", "lpg", "otomatik", "manuel", "vites"))
                return true;
            if (ContainsAny(n, "istanbul", "ankara", "izmir", "bursa", "antalya", "adana", "konya", "gaziantep"))
                return true;
            if (ContainsAny(n, "bmw", "mercedes", "audi", "toyota", "ford", "renault", "volkswagen", "vw", "honda", "hyundai", "fiat", "peugeot", "citroen", "opel", "nissan", "mazda", "kia", "dacia", "skoda", "seat", "cupra", "tesla"))
                return true;
            if (ContainsAny(n, "corolla", "civic", "passat", "jetta", "polo", "clio", "megane", "egea", "focus", "fiesta"))
                return true;
            if (ContainsAny(n, "bin tl", " milyon", " tl", "lira", "butce", "bütçe", "ucuz", "pahali", "pahalı"))
                return true;
            return false;
        }

        private static bool ContainsAny(string value, params string[] patterns)
            => patterns.Any(value.Contains);
    }
}
