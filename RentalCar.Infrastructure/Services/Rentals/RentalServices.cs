using Microsoft.EntityFrameworkCore;
using RentalCar.Domain.Entities;
using RentalCar.Domain.Rules;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services.Rentals;

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

    public async Task<Rental?> GetRentalByIdWithCarAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _rental.Rentals
            .AsNoTracking()
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task CreateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        RentalDateRules.ValidateSchedule(rental.StartDate, rental.RentalType, rental.Duration);

        var now = DateTime.UtcNow;
        if (rental.CreatedOn == default)
            rental.CreatedOn = now;
        if (rental.ModifiedOn == default)
            rental.ModifiedOn = now;

        await _rental.Rentals.AddAsync(rental, cancellationToken);
        await _rental.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteRentalAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _rental.Rentals.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return false;

        _rental.Rentals.Remove(entity);
        await _rental.SaveChangesAsync(cancellationToken);
        return true;
    }
}
