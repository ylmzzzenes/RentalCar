using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Dbcontexts;
using RentalCar.Data.Models;
using RentalCar.Data.Services;




namespace RentalCar
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
           
            builder.Services.AddDbContext<RentalCarContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RentalCarDb"));
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<RentalCarContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            { 
                options.LoginPath= "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";

            });

            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<CarServices>();
            builder.Services.AddScoped<RentalServices>();

            var app = builder.Build();

            

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("Home/Error");
            }
            app.UseStaticFiles();
            app.UseAuthorization();
            app.MapControllers();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole
                    >>();

                var roles = new[] { "Admin", "User", "Manager" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }

            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();


                string email = "admin@admin.com";
                string password = "Enes1234,";
                string firstName = "Enes,";
                string lastName = "Y?lmaz,";

                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user =new ApplicationUser();
                    user.UserName=email;
                    user.Email = email;
                    user.EmailConfirmed = true;
                    user.FirstName = firstName;
                    user.LastName = lastName;
                    
                    await userManager.CreateAsync(user, password);

                   await userManager.AddToRoleAsync(user, "Admin");  
                }
                
            }

            app.Run();
        }
    }
}
