using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Models;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services;

public class CarServices
{
    private readonly RentalCarContext _context;

    public CarServices(RentalCarContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Car CreateCar, CancellationToken cancellationToken = default)
    {
        await _context.Cars.AddAsync(CreateCar, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Car>> GetAllAsync(CancellationToken cancellation = default)
    {
        return await _context.Cars.ToListAsync(cancellation);
    }

    public async Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Cars.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _context.Cars.FindAsync(new object[] { id }, cancellationToken);
        if (car == null)
            return false;

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Car>> GetRandomAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        var allCars = await _context.Cars.ToListAsync(cancellationToken);
        var random = new Random();
        return allCars.OrderBy(_ => random.Next()).Take(count).ToList();
    }
}
