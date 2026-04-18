using Microsoft.Extensions.Logging;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Application.Validators.Cars;
using RentalCar.Core.Business;
using RentalCar.Core.Interceptors;
using RentalCar.Core.Utilities.Results.Abstract;
using RentalCar.Core.Utilities.Results.Concrete;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Enums;

namespace RentalCar.Application.Services.Cars;

public class CarAppService : ICarAppService
{
    private readonly ICarAiEnrichmentService _carAiEnrichmentService;
    private readonly ILogger<CarAppService> _logger;
    private static readonly CreateCarCommandValidator CommandValidator = new();

    public CarAppService(
        ICarAiEnrichmentService carAiEnrichmentService,
        ILogger<CarAppService> logger)
    {
        _carAiEnrichmentService = carAiEnrichmentService;
        _logger = logger;
    }

    [ValidationAspect(typeof(CreateCarRequestValidator))]
    public IResult Create(CreateCarRequest request)
    {
        var ruleResult = BusinessRules.Run(
            CheckIfBrandIsReserved(request.Brand),
            CheckIfModelNameIsTooGeneric(request.Model)
        );

        if (ruleResult is not null)
            return ruleResult;

        return new SuccessResult("Araç oluşturma doğrulama geçti.");
    }

    public IDataResult<Car> PrepareCarForCreate(CreateCarCommand command)
    {
        var fv = CommandValidator.Validate(command);
        if (!fv.IsValid)
            return new ErrorDataResult<Car>(string.Join(" ", fv.Errors.Select(e => e.ErrorMessage)));

        var marka = command.CatalogBrand?.Trim();
        var modelName = command.Model?.Trim();
        var ruleResult = BusinessRules.Run(
            CheckIfBrandIsReserved(marka ?? string.Empty),
            CheckIfModelNameIsTooGeneric(modelName ?? string.Empty));
        if (ruleResult is not null)
            return new ErrorDataResult<Car>(ruleResult.Message ?? "Doğrulama başarısız.");

        var car = new Car
        {
            Brand = marka,
            CatalogBrand = marka,
            CatalogModelName = modelName,
            Series = command.Series?.Trim(),
            ImageUrls = command.ImageUrls ?? new List<string>(),
            Model = modelName,
            ModelYear = command.ModelYear,
            Color = command.Color?.Trim(),
            OdometerKm = command.OdometerKm,
            EngineDisplacementLiters = command.EngineDisplacementLiters,
            ListedPrice = command.ListedPrice,
            EnginePowerHp = command.EnginePowerHp,
            BodyWorkNotes = command.BodyWorkNotes?.Trim(),
            FuelTankLiters = command.FuelTankLiters,
            VehicleCondition = command.VehicleCondition,
            TradeInAccepted = command.TradeInAccepted,
            SellerType = command.SellerType,
            FuelType = command.FuelType,
            Transmission = command.Transmission,
            Drivetrain = command.Drivetrain,
            BodyType = command.ListingBodyType != BodyType.None ? command.ListingBodyType : BodyType.None,
            Security = Security.None,
            InternalEquipment = InternalEquipment.None,
            ExternalEquipment = ExternalEquipment.None,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow
        };

        ApplyRentalFallbackPrices(car);

        return new SuccessDataResult<Car>(car);
    }

    private static void ApplyRentalFallbackPrices(Car car)
    {
        var listed = car.ListedPrice ?? 0;
        if (listed <= 0)
            return;

        if (car.DailyPrice <= 0)
            car.DailyPrice = Math.Round(listed / 30m, 2);
        if (car.WeeklyPrice <= 0)
            car.WeeklyPrice = Math.Round(listed / 4m, 2);
        if (car.MonthlyPrice <= 0)
            car.MonthlyPrice = listed;
    }

    public async Task<IDataResult<Car>> PrepareCarForCreateAsync(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var prepareResult = PrepareCarForCreate(command);
        if (!prepareResult.Success || prepareResult.Data is null)
            return new ErrorDataResult<Car>(prepareResult.Message ?? "Araç hazırlanamadı.");

        var car = prepareResult.Data;

        try
        {
            var ai = await _carAiEnrichmentService.EnrichAsync(car, cancellationToken);
            car.PredictedPriceMid = ai.MidPrice;
            car.PredictedPriceMin = ai.LowPrice;
            car.PredictedPriceMax = ai.HighPrice;
            car.ShortDescription = ai.ShortDescription;
            car.FullDescription = ai.FullDescription;
            if ((car.ImageUrls == null || car.ImageUrls.Count == 0) && ai.ImageUrls.Count > 0)
                car.ImageUrls = ai.ImageUrls;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI zenginleştirme atlandı; ilan temel verilerle kaydedilecek.");
        }

        return new SuccessDataResult<Car>(car);
    }

    private static IResult CheckIfBrandIsReserved(string brand)
    {
        var reservedBrands = new[] { "Test", "Demo", "Admin" };

        if (reservedBrands.Contains(brand, StringComparer.OrdinalIgnoreCase))
            return new ErrorResult("Bu marka adı sistem tarafından rezerve edilmiştir");

        return new SuccessResult();
    }

    private static IResult CheckIfModelNameIsTooGeneric(string model)
    {
        if (string.Equals(model, "Araba", StringComparison.OrdinalIgnoreCase))
            return new ErrorResult("Model adı çok genel olamaz");

        return new SuccessResult();
    }
}
