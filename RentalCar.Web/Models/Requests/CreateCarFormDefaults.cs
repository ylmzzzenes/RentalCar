using RentalCar.Domain.Enums;
using VehicleDriveType = RentalCar.Domain.Enums.DriveType;

namespace RentalCar.Web.Models.Requests;

/// <summary>Form açıldığında None (0) enum değerleri doğrulamada reddedilir; anlamlı varsayılanlar kullanılır.</summary>
public static class CreateCarFormDefaults
{
    public static CreateCar Create() => new()
    {
        Transmission = Gear.Manuel,
        FuelType = FuelType.Benzin,
        Drivetrain = VehicleDriveType.FWD,
        ListingBodyType = BodyType.Sedan,
        VehicleCondition = VehicleCondition.IkinciEl,
        SellerType = ListingSellerType.Sahibinden
    };
}
