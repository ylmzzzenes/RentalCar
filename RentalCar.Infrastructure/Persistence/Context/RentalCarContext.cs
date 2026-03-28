using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Models;

namespace RentalCar.Infrastructure.Persistence.Context
{
    public partial class RentalCarContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public virtual DbSet<Car> Cars { get; set; }

        public virtual DbSet<Rental> Rentals { get; set; }
        public virtual DbSet<Favorite> Favorites { get; set; }
        public virtual DbSet<CarRating> CarRatings { get; set; }
        public virtual DbSet<CarComment> CarComments { get; set; }

        public RentalCarContext() { }
        public RentalCarContext(DbContextOptions<RentalCarContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Favorite>()
                .HasIndex(x => new { x.CarId, x.UserId })
                .IsUnique();
            builder.Entity<Favorite>()
                .HasOne(x => x.Car)
                .WithMany()
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Favorite>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CarRating>()
                .HasIndex(x => new { x.CarId, x.UserId })
                .IsUnique();
            builder.Entity<CarRating>()
                .HasOne(x => x.Car)
                .WithMany()
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<CarRating>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CarComment>()
                .Property(x => x.Content)
                .HasMaxLength(1500);
            builder.Entity<CarComment>()
                .HasOne(x => x.Car)
                .WithMany()
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<CarComment>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Car>().Property(x => x.DailyPrice).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.WeeklyPrice).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.MonthlyPrice).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.fiyat).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.fiyat_tahmin).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.fiyat_min).HasPrecision(18, 2);
            builder.Entity<Car>().Property(x => x.fiyat_max).HasPrecision(18, 2);
            builder.Entity<Rental>().Property(x => x.Duration).HasPrecision(18, 2);
            builder.Entity<Rental>().Property(x => x.TotalPrice).HasPrecision(18, 2);
        }
    }
}
