using System.Text.Json.Serialization;

namespace RentalCar.Application.Dtos.AI
{
    public class PredictResponseDto
    {
        [JsonPropertyName("mid")]
        public decimal? Mid { get; set; }

        [JsonPropertyName("low")]
        public decimal? Low { get; set; }

        [JsonPropertyName("high")]
        public decimal? High { get; set; }

        [JsonPropertyName("prediction")]
        public decimal? Prediction { get; set; }
    }
}
