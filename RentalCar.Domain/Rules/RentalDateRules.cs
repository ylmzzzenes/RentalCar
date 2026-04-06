using RentalCar.Domain.Enums;

namespace RentalCar.Domain.Rules;

/// <summary>Kiralama başlangıç/bitiş ve süre kuralları (UTC).</summary>
public static class RentalDateRules
{
    public static decimal BillingCalendarDays(RentalType rentalType, decimal duration) =>
        rentalType switch
        {
            RentalType.Daily => duration,
            RentalType.Weekly => duration * 7,
            RentalType.Monthly => duration * 30,
            RentalType.LongTerm => duration * 365,
            _ => throw new ArgumentOutOfRangeException(nameof(rentalType), "Geçersiz kiralama tipi.")
        };

    public static DateTime ComputeEndUtc(DateTime startDateUtc, RentalType rentalType, decimal duration)
    {
        var days = BillingCalendarDays(rentalType, duration);
        return startDateUtc.AddDays((double)days);
    }

    /// <summary>Başlangıcın geçmişe düşmemesi, sürenin pozitif olması ve bitişin başlangıçtan sonra olması.</summary>
    public static void ValidateSchedule(DateTime startDateUtc, RentalType rentalType, decimal duration)
    {
        if (duration <= 0)
            throw new InvalidOperationException("Kiralama süresi sıfırdan büyük olmalıdır.");

        if (startDateUtc.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("Başlangıç tarihi DateTimeKind.Utc olmalıdır.");

        var todayUtc = DateTime.UtcNow.Date;
        if (startDateUtc.Date < todayUtc)
            throw new InvalidOperationException("Başlangıç tarihi bugünden önce olamaz.");

        var end = ComputeEndUtc(startDateUtc, rentalType, duration);
        if (end <= startDateUtc)
            throw new InvalidOperationException("Hesaplanan bitiş zamanı başlangıçtan sonra olmalıdır.");
    }
}
