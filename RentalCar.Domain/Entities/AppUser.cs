using Microsoft.AspNetCore.Identity;

namespace RentalCar.Domain.Entities;

public class AppUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
}
