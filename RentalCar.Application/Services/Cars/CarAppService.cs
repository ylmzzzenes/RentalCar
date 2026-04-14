using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Validators.Cars;
using RentalCar.Core.Business;
using RentalCar.Core.Interceptors;
using RentalCar.Core.Utilities.Results.Abstract;
using RentalCar.Core.Utilities.Results.Concrete;


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
