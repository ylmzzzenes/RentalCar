using RentalCar.Application.AI.Responses;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IAIService
    {
        Task<AiCompletionResult> ReplyAsync(string userMessage, string sessionId, CancellationToken cancellationToken = default);
    }
}
