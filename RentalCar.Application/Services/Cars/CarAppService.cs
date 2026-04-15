using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Validators.Cars;
using RentalCar.Core.Business;
using RentalCar.Core.Interceptors;
using RentalCar.Core.Utilities.Results.Abstract;
using RentalCar.Core.Utilities.Results.Concrete;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;


namespace RentalCar.Application.Services.Cars
{
    internal class CarAppService : ICarAppService
    {
        [ValidationAspect(typeof(CreateCarRequestValidator))]
        public IResult Create(CreateCarRequest request)
        {
            var ruleResult = BusinessRules.Run(
                CheckIfBrandIsReserved(request.Brand),
                CheckIfModelNameIsTooGeneric(request.Model)

                );

            if(ruleResult is not null)
            {
                return ruleResult;
            }

            return new SuccessResult("Araç oluşturma doğrulama geçti.");
        }

        public IDataResult<Car> PrepareCarForCreate(CreateCarCommand command)
        {

            var car = new Car
            {
                ImageUrls = command.ImageUrls,
                Model = command.Model,
                ModelYear = command.ModelYear,
                Color = command.Color,
                ModelRaw = command.ModelRaw,
                CatalogBrand = command.CatalogBrand,
                CatalogModelName = command.CatalogModelName,
                TrimPackage = command.TrimPackage,
                EngineCode = command.EngineCode,
                TransmissionCode = command.TransmissionCode,
                OdometerKm = command.OdometerKm,
                TaxAmount = command.TaxAmount,
                FuelConsumptionLPer100Km = command.FuelConsumptionLPer100Km,
                EngineDisplacementLiters = command.EngineDisplacementLiters,
                ListedPrice = command.ListedPrice,
                City = command.City,
                TrimLevelLabel = command.TrimLevelLabel,
                HasAccidentRecord = command.HasAccidentRecord,
                HasServiceHistory = command.HasServiceHistory,
                EnginePowerHp = command.EnginePowerHp,
                TorqueNm = command.TorqueNm,
                PreviousOwnerCount = command.PreviousOwnerCount,
                BodyStyleLabel = command.BodyStyleLabel,
                BodyWorkNotes = command.BodyWorkNotes,
                Security = MapFlags(command.SelectedSecurity, Security.None),
                InternalEquipment = MapFlags(command.SelectedInternal, InternalEquipment.None),
                ExternalEquipment = MapFlags(command.SelectedExternal, ExternalEquipment.None),
                BodyType = MapFlags(command.SelectedBodyType, BodyType.None),
                FuelType = MapFlags(command.SelectedFuelType, FuelType.None),
                Transmission = MapFlags(command.SelectedGear, Gear.None),
                Drivetrain = MapFlags(command.SelectedDriveType, RentalCar.Domain.Enums.DriveType.None),
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            };

            if (car.FuelType == FuelType.None && command.FuelType != FuelType.None)
                car.FuelType = command.FuelType;

            if (car.Transmission == Gear.None && command.Transmission != Gear.None)
                car.Transmission = command.Transmission;

            if (car.Drivetrain == Domain.Enums.DriveType.None && command.Drivetrain != Domain.Enums.DriveType.None)
                car.Drivetrain = command.Drivetrain;

            return new SuccessDataResult<Car>(car);
        }

        private static TEnum MapFlags<TEnum>(List<int>? values, TEnum noneValue) where TEnum: struct, Enum
        {
            if (values == null || values.Count == 0)
                return noneValue;

            var combined = Convert.ToInt32(noneValue);

            foreach(var value in values)
            {
                combined |= value;
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), combined);
        } 

        private IResult CheckIfBrandIsReserved(string brand)
        {
            var reservedBrands = new[] { "Test", "Demo", "Admin" };

            if (reservedBrands.Contains(brand, StringComparer.OrdinalIgnoreCase)) 
            {
                return new ErrorResult("Bu marka adı sistem tarafından rezerve edilmiştir");
            }

            return new SuccessResult();
        }

        private IResult CheckIfModelNameIsTooGeneric(string model)
        {
            if(string.Equals(model, "Araba", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorResult("Model adı çok genel olamaz");
            }

            return new SuccessResult();
        }
    }
}
