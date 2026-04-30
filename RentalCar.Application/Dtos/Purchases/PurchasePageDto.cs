namespace RentalCar.Application.Dtos.Purchases;

public sealed class PurchasePageDto
{
    public int CarId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? ModelYear { get; set; }
    public decimal? ListedPrice { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
