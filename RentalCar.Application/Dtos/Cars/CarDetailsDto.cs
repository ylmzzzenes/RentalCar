using RentalCar.Domain.Entities;

namespace RentalCar.Application.Dtos.Cars
{
    public sealed class CarDetailsDto
    {
        public Car Car { get; set; } = default!;
        public List<CarCommentItemDto> Comments { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int? CurrentUserRating { get; set; }
        public bool IsFavorite { get; set; }

    }
}
