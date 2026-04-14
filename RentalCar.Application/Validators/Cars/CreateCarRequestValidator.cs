using FluentValidation;
using RentalCar.Application.Contracts.Cars;

namespace RentalCar.Application.Validators.Cars
{
    public class CreateCarRequestValidator: AbstractValidator<CreateCarRequest>
    {
        public CreateCarRequestValidator()
        {
            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("Marka alanı boş bırakılamaz")
                .MaximumLength(50).WithMessage("Marka alanı en fazla 50 karakter olabilir");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("Model alanı boş bırakılamaz")
                .MaximumLength(50).WithMessage("Model alanı en fazla 50 karakter olabilir");

            RuleFor(x => x.PricePerDay )
                .GreaterThan(0).WithMessage("Günlük fiyat 0'dan büyük olmalıdır");

            RuleFor(x => x.Year)
                .InclusiveBetween(1950,DateTime.Now.Year+1)
                .WithMessage($"Yıl 1950 ile {DateTime.Now.Year + 1} arasında olmalıdır");

            RuleFor(x => x.FuelType)
                .NotEmpty().WithMessage("Yakıt tipi şeçilmelidir");

            RuleFor(x => x.Transmission)
                .NotEmpty().WithMessage("Vites tipi seçilmelidir");


        }
    }
}
