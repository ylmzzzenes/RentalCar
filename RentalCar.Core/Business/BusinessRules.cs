using RentalCar.Core.Utilities.Results.Abstract;

namespace RentalCar.Core.Business;

public static class BusinessRules
{
    public static IResult? Run(params IResult[] logics)
    {
        foreach (var logic in logics)
        {
            if (!logic.Success)
            {
                return logic;
            }
        }

        return null;
    }
}