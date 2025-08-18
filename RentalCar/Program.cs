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

            builder.Services.AddIdentity<AppUser, AppRole>().
                AddEntityFrameworkStores<RentalCarContext>().
                AddDefaultTokenProviders();

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


            app.Run();
        }
    }
}
