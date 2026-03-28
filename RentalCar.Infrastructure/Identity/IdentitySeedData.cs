using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalCar.Infrastructure.Persistence.Context;

namespace RentalCar.Data.Models
{
    public class IdentitySeedData
    {
        private const string adminUser = "Admin";
        private const string adminPassword = "Admin1234,";
        private const string adminEmail = "admin@enesyilmaz.com";

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

            var user = await userManager.FindByNameAsync(adminUser);
            if (user == null)
            {
                user = new AppUser
                {
                    FullName = "Enes Yılmaz",
                    UserName = adminUser,
                    Email = adminEmail,
                    PhoneNumber = "05458309222"
                };

                await userManager.CreateAsync(user, adminPassword);
            }

            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
}
