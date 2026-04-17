using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RentalCar.Domain.Entities;

namespace RentalCar.Infrastructure.Persistence.Context;

public partial class RentalCarContext : IdentityDbContext<AppUser, AppRole, string>
{
    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }
    public virtual DbSet<Purchase> Purchases { get; set; }
    public virtual DbSet<Favorite> Favorites { get; set; }
    public virtual DbSet<CarRating> CarRatings { get; set; }
    public virtual DbSet<CarComment> CarComments { get; set; }

    public RentalCarContext() { }

    public RentalCarContext(DbContextOptions<RentalCarContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var imageUrlConverter = new ValueConverter<List<string>, string>(
            v => string.Join(',', v.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : v.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0)
                    .ToList());

        var imageUrlComparer = new ValueComparer<List<string>>(
            (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b, StringComparer.Ordinal)),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, StringComparer.Ordinal.GetHashCode(s ?? string.Empty))),
            v => v.ToList());

        builder.Entity<Car>()
            .Property(c => c.ImageUrls)
            .HasConversion(imageUrlConverter, imageUrlComparer)
            .IsRequired(false);

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

        builder.Entity<Car>()
            .HasOne(c => c.PostedBy)
            .WithMany()
            .HasForeignKey(c => c.PostedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Car>().Property(x => x.DailyPrice).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.WeeklyPrice).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.ListedPrice).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.PredictedPriceMid).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.PredictedPriceMin).HasPrecision(18, 2);
        builder.Entity<Car>().Property(x => x.PredictedPriceMax).HasPrecision(18, 2);
        builder.Entity<Rental>().Property(x => x.Duration).HasPrecision(18, 2);
        builder.Entity<Rental>().Property(x => x.TotalPrice).HasPrecision(18, 2);

        builder.Entity<Rental>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Purchase>().Property(x => x.AgreedPrice).HasPrecision(18, 2);
        builder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Purchase>()
            .HasOne(p => p.Car)
            .WithMany()
            .HasForeignKey(p => p.CarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
