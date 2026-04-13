using FluentValidation;
using RentalCar.Core.Exceptions;


namespace RentalCar.Web.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> _logger)
        {
            _next = next;
            _logger = _logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "Unhandled exception occured");

                await HandleExceptionAsync(context, exception);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var message = "Beklenmeyen bir hata oluştu";

            switch (exception)
            {
                case ValidationException validationException:
                    message = validationException.Errors.FirstOrDefault()?.ErrorMessage ?? "Doğrulama hatası oluştu";
                    break;
                case BusinessException businessException:
                    message = businessException.Message;
                    break;
                default:
                    message = "Beklenmeyen bir hata oluştu";
                    break;
            }

            votext.Items["ExceptionMessage"] = message;

            return Task.FromException(exception);
        }
    }
}
