using RentalCar.Domain.Enums;

namespace RentalCar.Application.Dtos.Rentals;

public sealed class RentalPageDto
{
    public int CarId { get; set; }

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;

    public int? ModelYear { get; set; }

    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public string Security { get; set; } = string.Empty;
    public string InternalEquipment { get; set; } = string.Empty;
    public string ExternalEquipment { get; set; } = string.Empty;

    public List<string> ImageUrls { get; set; } = new();

    public decimal DailyPrice { get; set; }
    public decimal WeeklyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }

    public RentalType RentalType { get; set; }
    public int Duration { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
}