using RentalCar.Web.Extensions;




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
                options.AccessDeniedPath = "/Account/AccessDenied";
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
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(45);
            });

            builder.Services.AddScoped<CarServices>();
            builder.Services.AddScoped<RentalServices>();

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
            builder.Services.AddScoped<IRentalAppService, RentalAppService>();

            var aiBaseUrl = builder.Configuration["AiApi:BaseUrl"] ?? "http://localhost:8000";
            builder.Services.AddHttpClient<PricingApiClient>(client =>
            {
                client.BaseAddress = new Uri(aiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient<DescriptionService>(client =>
            {
                client.BaseAddress = new Uri(aiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var app = builder.Build();

            app.UseCustomExceptionMiddleware();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
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


            app.Run();
        }
    }
}
