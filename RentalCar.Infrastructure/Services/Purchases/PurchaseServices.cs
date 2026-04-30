using Microsoft.EntityFrameworkCore;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Services.Purchases;

public class PurchaseServices
{
    private readonly RentalCarContext _context;

    public PurchaseServices(RentalCarContext context)
    {
        _context = context;
    }

    public async Task<Car?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Cars.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task CreateAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        await _context.Purchases.AddAsync(purchase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
