using RentalCar.Domain.Entities;

namespace RentalCar.Application.Dtos.Rentals
{
    public sealed class RentalResultDto
    {
        public bool Success { get; set; } 
        public string? ErrorMessage { get; set; }
        public Rental? Rental { get; set; }

    }
}
