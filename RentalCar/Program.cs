using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalCar.Data.Dbcontexts;
using RentalCar.Data.Models;
using RentalCar.Data.Services;




namespace RentalCar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
           
            builder.Services.AddDbContext<RentalCarContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RentalCarDb"));
            });

            builder.Services.AddIdentity<AppUser, AppRole>().
                AddEntityFrameworkStores<RentalCarContext>().
                AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options =>
            {

                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Index";
                options.AccessDeniedPath = "/Account/AccesDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });


            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i =>
            {
                var host = builder.Configuration["EmailSender:Host"]
                    ?? throw new InvalidOperationException("EmailSender:Host yapılandırılmamış.");
                var userName = builder.Configuration["EmailSender:UserName"]
                    ?? throw new InvalidOperationException("EmailSender:UserName yapılandırılmamış.");
                var password = builder.Configuration["EmailSender:Password"]
                    ?? throw new InvalidOperationException("EmailSender:Password yapılandırılmamış.");

                return new SmtpEmailSender(
                host,
                builder.Configuration.GetValue<int>("EmailSender:Port"),
                builder.Configuration.GetValue<bool>("EmailSender:EnableSSL"),
                userName,
                password

            );
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


            app.Run();
        }
    }
}
