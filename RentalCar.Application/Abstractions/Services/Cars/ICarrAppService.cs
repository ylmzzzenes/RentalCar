using RentalCar.Application.Contracts.Cars;
using RentalCar.Core.Utilities.Results.Abstract;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface ICarAppService
{
    IResult Create(CreateCarRequest request);
}