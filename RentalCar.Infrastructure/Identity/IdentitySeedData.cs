using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalCar.Domain.Entities;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Infrastructure.Identity;

public static class IdentitySeedData
{
    private const string AdminUser = "Admin";
    private const string AdminPassword = "Admin1234,";
    private const string AdminEmail = "admin@enesyilmaz.com";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var context = scopedProvider.GetRequiredService<RentalCarContext>();
        await context.Database.MigrateAsync();

        var roleManager = scopedProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scopedProvider.GetRequiredService<UserManager<AppUser>>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
            }
        }

        var user = await userManager.FindByNameAsync(AdminUser);
        if (user == null)
        {
            user = new AppUser
            {
                FullName = "Enes Yılmaz",
                UserName = AdminUser,
                Email = AdminEmail,
                PhoneNumber = "05458309222"
            };

            await userManager.CreateAsync(user, AdminPassword);
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
