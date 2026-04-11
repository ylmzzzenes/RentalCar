using RentalCar.Core.Utilities.Results.Abstract;

namespace RentalCar.Core.Utilities.Results.Concrete
{
    public class Result : IResult
    {
        public Result(bool success, string message) : this(success)
        {
            Message = message;
        }

        public Result(bool success)
        {
            Success = success;
            Message = string.Empty;
        }

        public bool Success { get; }

        public string Message { get; }
    }
}
