using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Models;

namespace RentalCar.Data.Dbcontexts
{
    public partial class RentalCarContext :IdentityDbContext<AppUser,AppRole,string>
    {
        public virtual DbSet<Car> Cars { get; set; }

        public virtual DbSet<Rental> Rentals { get; set; }
      
        public RentalCarContext() { }
        public  RentalCarContext(DbContextOptions<RentalCarContext> options)
            :base(options ){}
      


    }
}
