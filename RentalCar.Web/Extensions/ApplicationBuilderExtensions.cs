using RentalCar.Web.Middlewares;

namespace RentalCar.Web.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomExceptionMiddleware(this IApplicationBuilder app)
        {
            return app.UserMiddleWare<ExceptionMiddleware>();
        }
    }
}
