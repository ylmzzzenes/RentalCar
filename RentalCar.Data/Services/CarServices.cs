using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Dbcontexts;
using RentalCar.Data.Enums;
using RentalCar.Data.Models;


namespace RentalCar.Data.Services
{
    public class CarServices
    {
        private readonly RentalCarContext _context;

        public CarServices(RentalCarContext context) {
            _context = context;
        }

        public async Task CreateAsync(Car CreateCar,CancellationToken cancellationToken = default)
        {
            await _context.Cars.AddAsync(CreateCar,cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Car>> GetAllAsync(CancellationToken cancellation = default)
        {
            return await _context.Cars.ToListAsync(cancellation);
        }

        public async Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Cars.FindAsync(new object[] {id},cancellationToken);
        }

        public async Task UpdateAsync(Car car ,CancellationToken cancellationToken = default)
        {
            _context.Cars.Update(car);
            await _context.SaveChangesAsync(cancellationToken) ;
        }

        public async Task <bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var car =await _context.Cars.FindAsync(new object[] {id},cancellationToken);
            if (car == null)
                return false;

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<Car>> GetRandomAsync(int count = 5)
        {
            var allCars= await _context.Cars.ToListAsync();
            var random = new Random();
            var randomCars = allCars.OrderBy(x => random.Next()).Take(count).ToList();

            return randomCars;
        }

        



    }
}
