using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace RentalCar.Data.Dbcontexts
{
    public class RentalCarContextFactory:IDesignTimeDbContextFactory<RentalCarContext>
    {
        public RentalCarContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RentalCarContext>();
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RentalCar;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;");

            return new RentalCarContext(optionsBuilder.Options);
        } 
    }
}
