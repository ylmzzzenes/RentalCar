namespace RentalCar.Data.Models
{
    public class CarRating : BaseEntity
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int Score { get; set; } // 1-5

        public Car Car { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
