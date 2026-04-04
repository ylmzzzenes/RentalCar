using RentalCar.Domain.Common;

namespace RentalCar.Domain.Entities;

public class CarComment : BaseEntity
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Car Car { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
