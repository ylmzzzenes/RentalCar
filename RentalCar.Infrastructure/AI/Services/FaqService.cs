using RentalCar.Application.Abstractions.AI;

namespace RentalCar.AI.Services
{
    public class FaqService : IFaqService
    {
        private static readonly Dictionary<string, string> Faq = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ehliyet"] = "Arac kiralama icin gecerli surucu belgesi ve kimlik gereklidir.",
            ["depozito"] = "Arac tipine gore degisen depozito tutari kredi kartindan provizyon olarak alinabilir.",
            ["iptal"] = "Rezervasyon iptali, kiralama baslamadan once panelden yapilabilir.",
            ["iade"] = "Arac, sozlesmede belirtilen saat ve lokasyonda iade edilmelidir.",
            ["sigorta"] = "Standart trafik sigortasi dahildir. Ek paketler arac tipine gore degisebilir."
        };

        public object Search(string question)
        {
            var q = (question ?? string.Empty).ToLowerInvariant();
            var match = Faq.FirstOrDefault(x => q.Contains(x.Key));
            if (!string.IsNullOrWhiteSpace(match.Key))
            {
                return new { found = true, topic = match.Key, answer = match.Value };
            }

            return new
            {
                found = false,
                answer = "Bu konu icin net bir bilgi bulamadim. Musteri hizmetleriyle iletisime gecebilirsiniz."
            };
        }
    }
}
