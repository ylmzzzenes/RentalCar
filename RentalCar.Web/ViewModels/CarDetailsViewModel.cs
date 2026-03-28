using RentalCar.Data.Models;

namespace RentalCar.ViewModels
{
    public class CarDetailsViewModel
    {
        public Car Car { get; set; } = null!;
        public bool IsFavorite { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int? CurrentUserRating { get; set; }
        public List<CarComment> Comments { get; set; } = new();
    }
}
