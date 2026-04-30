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

    public async Task<bool> HasOverlappingRentalAsync(int carId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var rows = await _rental.Rentals
            .AsNoTracking()
            .Where(r =>
                r.CarId == carId &&
                r.Status != Domain.Enums.RentalStatus.Cancelled)
            .Select(r => new { r.StartDate, r.RentalType, r.Duration })
            .ToListAsync(cancellationToken);

        foreach (var r in rows)
        {
            var rStart = r.StartDate.Kind == DateTimeKind.Utc
                ? r.StartDate
                : DateTime.SpecifyKind(r.StartDate, DateTimeKind.Utc);
            var rEnd = RentalDateRules.ComputeEndUtc(rStart, r.RentalType, r.Duration);
            if (startDate < rEnd && endDate > rStart)
                return true;
        }

        return false;
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
