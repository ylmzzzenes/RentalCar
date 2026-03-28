using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Models;

namespace RentalCar.AI.Services
{
    public class ConversationMemoryService : IConversationMemoryService
    {
        private const int MaxMessages = 12;
        private const string Prefix = "ai_chat_history_";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConversationMemoryService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<List<ConversationMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session is null) return Task.FromResult(new List<ConversationMessage>());

            var raw = session.GetString(Prefix + sessionId);
            if (string.IsNullOrWhiteSpace(raw)) return Task.FromResult(new List<ConversationMessage>());

            var list = JsonSerializer.Deserialize<List<ConversationMessage>>(raw) ?? new List<ConversationMessage>();
            return Task.FromResult(list.OrderBy(x => x.CreatedAtUtc).TakeLast(MaxMessages).ToList());
        }

        public Task AppendAsync(string sessionId, string role, string content, CancellationToken cancellationToken = default)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session is null) return Task.CompletedTask;

            var key = Prefix + sessionId;
            var raw = session.GetString(key);
            var list = string.IsNullOrWhiteSpace(raw)
                ? new List<ConversationMessage>()
                : JsonSerializer.Deserialize<List<ConversationMessage>>(raw) ?? new List<ConversationMessage>();

            list.Add(new ConversationMessage
            {
                Role = role,
                Content = content,
                CreatedAtUtc = DateTime.UtcNow
            });

            list = list.OrderBy(x => x.CreatedAtUtc).TakeLast(MaxMessages).ToList();
            session.SetString(key, JsonSerializer.Serialize(list));
            return Task.CompletedTask;
        }
    }
}
