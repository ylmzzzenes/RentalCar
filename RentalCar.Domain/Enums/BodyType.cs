using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums
{
    [Flags]
    public enum BodyType
    {
        [Display(Name = "Seçiniz")]
        None = 0,

        [Display(Name = "Sedan")]
        Sedan = 1,

        [Display(Name = "Coupe")]
        Coupe = 2,

        [Display(Name = "Spor")]
        SportsCar = 4,

        [Display(Name = "Station wagon")]
        StationWagon = 8,

        [Display(Name = "Hatchback")]
        Hatchback = 16,

        [Display(Name = "SUV")]
        Suv = 32,

        [Display(Name = "Minivan")]
        Minivan = 64,

        [Display(Name = "Pick-up")]
        PıckUp = 128
    }
}
