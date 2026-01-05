using Microsoft.AspNetCore.Mvc;
using RentalCar.Data.Enums;
using DriveType = RentalCar.Data.Enums.DriveType;

namespace RentalCar.ViewComponents
{
    public class EnumSelectorViewComponent: ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new
            {
                Security = Enum.GetValues(typeof(Security)).Cast<Security>().Where(e => e != Security.None).ToList(),
                InternalEquipment = Enum.GetValues(typeof(InternalEquipment)).Cast<InternalEquipment>().Where(e => e != InternalEquipment.None).ToList(),
                ExternalEquipment = Enum.GetValues(typeof(ExternalEquipment)).Cast<ExternalEquipment>().Where(e => e != ExternalEquipment.None).ToList(),
                FuelType = Enum.GetValues(typeof(FuelType)).Cast<FuelType>().Where(e => e != FuelType.None).ToList(),
                Gear = Enum.GetValues(typeof(Gear)).Cast<Gear>().Where(e => e != Gear.None).ToList(),
                BodyType = Enum.GetValues(typeof(BodyType)).Cast<BodyType>().Where(e => e != BodyType.None).ToList(),
                DriveType = Enum.GetValues(typeof(DriveType)).Cast<DriveType>().Where(e => e != DriveType.None).ToList()
            };

            return View(model);
        }
    }
}
