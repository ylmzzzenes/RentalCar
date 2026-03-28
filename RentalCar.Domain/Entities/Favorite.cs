namespace RentalCar.Data.Models
{
    public class Favorite : BaseEntity
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string UserId { get; set; } = string.Empty;

        public Car Car { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
