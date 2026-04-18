using FluentValidation;
using RentalCar.Application.Contracts.Cars;
using RentalCar.Domain.Enums;
using VehicleDriveType = RentalCar.Domain.Enums.DriveType;

namespace RentalCar.Application.Validators.Cars;

public sealed class CreateCarCommandValidator : AbstractValidator<CreateCarCommand>
{
    public CreateCarCommandValidator()
    {
        RuleFor(x => x.CatalogBrand)
            .NotEmpty().WithMessage("Marka zorunludur.")
            .MaximumLength(120).WithMessage("Marka en fazla 120 karakter olabilir.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model zorunludur.")
            .MaximumLength(200).WithMessage("Model en fazla 200 karakter olabilir.");

        RuleFor(x => x.ListedPrice)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır.");

        RuleFor(x => x.ModelYear)
            .InclusiveBetween(1950, DateTime.UtcNow.Year + 1)
            .When(x => x.ModelYear.HasValue)
            .WithMessage($"Yıl 1950–{DateTime.UtcNow.Year + 1} arasında olmalıdır.");

        RuleFor(x => x.OdometerKm)
            .InclusiveBetween(0, 2_000_000)
            .When(x => x.OdometerKm.HasValue)
            .WithMessage("Kilometre geçerli aralıkta olmalıdır.");

        RuleFor(x => x.FuelType)
            .NotEqual(FuelType.None).WithMessage("Yakıt tipi seçiniz.");

        RuleFor(x => x.Transmission)
            .NotEqual(Gear.None).WithMessage("Vites tipi seçiniz.");

        RuleFor(x => x.Drivetrain)
            .NotEqual(VehicleDriveType.None).WithMessage("Çekiş tipi seçiniz.");

        RuleFor(x => x.ListingBodyType)
            .NotEqual(BodyType.None).WithMessage("Kasa tipi seçiniz.");
    }
}
