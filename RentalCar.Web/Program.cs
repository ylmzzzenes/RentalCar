using RentalCar.Web.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using RentalCar.Application.DependencyResolvers.Autofac;
using RentalCar.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using RentalCar.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using RentalCar.Application.Abstractions.Services;
using RentalCar.Infrastructure.Services.Email;
using RentalCar.Infrastructure.Services.Cars;
using RentalCar.Infrastructure.Services.Rentals;
using RentalCar.AI.Configuration;
using RentalCar.Application.Abstractions.AI;
using RentalCar.AI.Services;
using RentalCar.Application.Abstractions.Services.Cars;
using RentalCar.Infrastructure.AI.Services;
using System.Net.Http.Headers;
using RentalCar.Infrastructure.Identity;
using RentalCar.Infrastructure.Services.Purchases;
using Serilog;

namespace RentalCar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "rentalcar-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            // Autofac'i ana DI container olarak kullan
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            { 
                containerBuilder.RegisterModule(new AutofacApplicationModule());
            });

            // DbContext
            builder.Services.AddDbContext<RentalCarContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RentalCarDb"));
            });

            // Identity
            builder.Services.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<RentalCarContext>()
                .AddDefaultTokenProviders();

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
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

            // MVC
            builder.Services.AddControllersWithViews();

            // HttpContext / Cache / Session
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(45);
            });

            // Uygulama servisleri
            builder.Services.AddScoped<CarServices>();
            builder.Services.AddScoped<RentalServices>();
            builder.Services.AddScoped<PurchaseServices>();
            builder.Services.AddScoped<IEmailSender>(_ =>
            {
                var host = builder.Configuration["EmailSender:Host"]
                    ?? throw new InvalidOperationException("EmailSender:Host yapılandırılmamış.");
                var userName = builder.Configuration["EmailSender:UserName"]
                    ?? throw new InvalidOperationException("EmailSender:UserName yapılandırılmamış.");
                var password = builder.Configuration["EmailSender:Password"]
                    ?? throw new InvalidOperationException("EmailSender:Password yapılandırılmamış.");
                var port = builder.Configuration.GetValue<int>("EmailSender:Port");
                var enableSsl = builder.Configuration.GetValue<bool>("EmailSender:EnableSsl");

                return new SmtpEmailSender(host, port, enableSsl, userName, password);
            });

            // AI ayarları
            builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
            builder.Services.PostConfigure<OpenAiOptions>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
                }
            });

            builder.Services.AddHttpClient<IAIService, AIService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(35);
            });

            builder.Services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
            builder.Services.AddScoped<IIntentClassifier, IntentClassifier>();
            builder.Services.AddScoped<IToolLayerService, ToolLayerService>();
            builder.Services.AddScoped<IRentalService, RentalService>();
            builder.Services.AddScoped<IRecommendationService, RecommendationService>();
            builder.Services.AddScoped<IPricingService, PricingService>();
            builder.Services.AddScoped<IFaqService, FaqService>();
            builder.Services.AddScoped<ICarInteractionService, CarInteractionService>();
            builder.Services.AddHttpClient(nameof(CarCatalogImageSyncService));

            var aiBaseUrl = builder.Configuration["AiApi:BaseUrl"] ?? "http://localhost:8000";

            builder.Services.AddHttpClient<PricingApiClient>(client =>
            {
                client.BaseAddress = new Uri(aiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient<DescriptionService>(client =>
            {
                client.BaseAddress = new Uri(aiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var app = builder.Build();

            // Hata yakalama
            app.UseExceptionHandler("/Home/Error");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<RentalCarContext>();
                db.Database.Migrate();
            }

            IdentitySeedData.SeedAsync(app.Services).GetAwaiter().GetResult();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            try
            {
                app.Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}