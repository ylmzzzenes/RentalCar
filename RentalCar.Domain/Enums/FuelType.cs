using System.ComponentModel.DataAnnotations;

namespace RentalCar.Domain.Enums
{
    [Flags]
    public enum FuelType
    {
        [Display(Name = "Seçiniz")]
        None = 0,

        [Display(Name = "Dizel")]
        Dizel = 1,

        [Display(Name = "Elektrik")]
        Elektrik = 2,

        [Display(Name = "Hibrit")]
        Hibrit = 4,

        [Display(Name = "Benzin")]
        Benzin = 8
    }
}
