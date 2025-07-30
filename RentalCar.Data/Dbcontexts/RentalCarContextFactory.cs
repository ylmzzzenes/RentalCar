using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace RentalCar.Data.Dbcontexts
{
    public class RentalCarContextFactory:IDesignTimeDbContextFactory<RentalCarContext>
    {
        public RentalCarContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RentalCarContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\enesdb;Database=enesyilmazdb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new RentalCarContext(optionsBuilder.Options);
        } 
    }
}
