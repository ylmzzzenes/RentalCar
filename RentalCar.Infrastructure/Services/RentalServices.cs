using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Models;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services;

public class RentalServices
{
    private readonly RentalCarContext _rental;

    public RentalServices(RentalCarContext rental)
    {
        _rental = rental;
    }

    public async Task<Car?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _rental.Cars.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Car>> GetRentalAllAsync(CancellationToken cancellation = default)
    {
        return await _rental.Cars.ToListAsync(cancellation);
    }

    public async Task CreateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        await _rental.Rentals.AddAsync(rental, cancellationToken);
        await _rental.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteRentalAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _rental.Cars.FindAsync(new object[] { id }, cancellationToken);
        if (car == null)
            return false;

        _rental.Cars.Remove(car);
        await _rental.SaveChangesAsync(cancellationToken);
        return true;
    }
}
