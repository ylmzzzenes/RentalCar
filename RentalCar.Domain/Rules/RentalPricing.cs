using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;

namespace RentalCar.Domain.Rules;

public static class RentalPricing
{
    /// <summary>Kiralama toplam tutarı (birim fiyat × süre). Fiyatlar 0 ise ilan fiyatından türetilir.</summary>
    public static decimal ComputeTotal(Car car, RentalType rentalType, decimal duration)
    {
        if (duration <= 0)
            return 0;

        var listed = car.ListedPrice ?? 0;
        var daily = car.DailyPrice > 0 ? car.DailyPrice : (listed > 0 ? Math.Round(listed / 30m, 2) : 0);
        var weekly = car.WeeklyPrice > 0 ? car.WeeklyPrice : (listed > 0 ? Math.Round(listed / 4m, 2) : daily * 7);
        var monthly = car.MonthlyPrice > 0 ? car.MonthlyPrice : (listed > 0 ? listed : daily * 30);

        var unit = rentalType switch
        {
            RentalType.Daily => daily,
            RentalType.Weekly => weekly,
            RentalType.Monthly => monthly,
            RentalType.LongTerm => monthly * 12m,
            _ => daily
        };

        return Math.Round(unit * duration, 2);
    }
}
