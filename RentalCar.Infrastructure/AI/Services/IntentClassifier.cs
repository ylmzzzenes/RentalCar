using RentalCar.Application.Abstractions.AI;

namespace RentalCar.AI.Services
{
    public class IntentClassifier : IIntentClassifier
    {
        public string Classify(string message)
        {
            var normalized = (message ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return "fallback";

            if (ContainsAny(normalized, "fiyat", "pahali", "ucuz", "kac tl", "hesapla"))
                return "pricing";
            if (ContainsAny(normalized, "oner", "tavsiye", "en mantikli", "uygun arac"))
                return "recommendation";
            if (ContainsAny(normalized, "rezerv", "kirala", "book", "yardim"))
                return "booking_help";
            if (ContainsAny(normalized, "sss", "iade", "iptal", "sigorta", "ehliyet", "depozito"))
                return "faq";
            if (ContainsAny(normalized, "bul", "ara", "istanbul", "ankara", "suv", "sedan"))
                return "car_search";

            return "fallback";
        }

        private static bool ContainsAny(string value, params string[] patterns)
            => patterns.Any(value.Contains);
    }
}
