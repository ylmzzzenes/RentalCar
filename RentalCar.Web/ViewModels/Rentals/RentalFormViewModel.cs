using RentalCar.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace RentalCar.Web.ViewModels.Rentals
{
    public sealed class RentalFormViewModel
    {
        public int CarId { get; set; }

        [Required(ErrorMessage = "Kiralama türü zorunludur.")]
        public RentalType RentalType { get; set; } = RentalType.Daily;

        [Range(1, int.MaxValue, ErrorMessage = "Süre 1 veya daha büyük olmalıdır.")]
        public int Duration { get; set; } = 1;
    }
}
