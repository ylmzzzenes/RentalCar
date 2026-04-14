namespace RentalCar.Application.Contracts.Cars;

public class CreateCarRequest
{
    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public decimal PricePerDay { get; set; }

    public int Year { get; set; }

    public string FuelType { get; set; } = string.Empty;

    public string Transmission { get; set; } = string.Empty;
}