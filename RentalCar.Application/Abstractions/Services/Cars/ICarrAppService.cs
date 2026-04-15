using RentalCar.Application.Contracts.Cars;
using RentalCar.Core.Utilities.Results.Abstract;
using RentalCar.Domain.Entities;

namespace RentalCar.Application.Abstractions.Services.Cars;

public interface ICarAppService
{
    IResult Create(CreateCarRequest request);
    IDataResult<Car> PrepareCarForCreate(CreateCarCommand command); 
}