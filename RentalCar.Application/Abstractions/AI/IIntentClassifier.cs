namespace RentalCar.Application.Abstractions.AI
{
    public interface IIntentClassifier
    {
        string Classify(string message);
    }
}
