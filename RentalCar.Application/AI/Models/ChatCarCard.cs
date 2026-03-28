namespace RentalCar.Application.AI.Models
{
    public class ChatCarCard
    {
        public int CarId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? City { get; set; }
        public decimal Price { get; set; }
        public string PriceUnit { get; set; } = "TL/gun";
        public string? ImageUrl { get; set; }
        public string? FuelType { get; set; }
        public string? BodyType { get; set; }
    }
}
