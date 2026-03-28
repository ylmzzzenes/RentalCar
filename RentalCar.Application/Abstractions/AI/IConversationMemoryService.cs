using RentalCar.Application.AI.Models;

namespace RentalCar.Application.Abstractions.AI
{
    public interface IConversationMemoryService
    {
        Task<List<ConversationMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
        Task AppendAsync(string sessionId, string role, string content, CancellationToken cancellationToken = default);
    }
}
