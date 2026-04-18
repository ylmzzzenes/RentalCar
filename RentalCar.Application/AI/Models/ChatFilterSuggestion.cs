namespace RentalCar.Application.AI.Models
{
    public class ChatFilterSuggestion
    {
        /// <summary>Serbest metin: marka, model, renk vb.</summary>
        public string? Query { get; set; }
        public string? City { get; set; }
        public string? VehicleType { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? FuelType { get; set; }
    }
}
