using RentalCar.Domain.Enums;

namespace RentalCar.Domain.Entities;

public class Rental
{
    public int Id { get; set; }

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public RentalType RentalType { get; set; }

    public decimal Duration { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate => StartDate.AddDays((double)CalculateTotalDays);

    private decimal CalculateTotalDays =>
        RentalType switch
        {
            RentalType.Daily => Duration,
            RentalType.Weekly => Duration * 7,
            RentalType.Monthly => Duration * 30,
            RentalType.LongTerm => Duration * 365,
            _ => throw new ArgumentOutOfRangeException(nameof(RentalType), "Invalid rental type")
        };
}
