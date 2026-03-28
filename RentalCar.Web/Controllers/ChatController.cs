using Microsoft.AspNetCore.Mvc;
using RentalCar.Application.Abstractions.AI;
using RentalCar.Application.AI.Requests;
using RentalCar.Application.AI.Responses;

namespace RentalCar.Controllers
{
    [Route("chat")]
    public class ChatController : Controller
    {
        private readonly IAIService _aiService;

        public ChatController(IAIService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("message")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Message([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Mesaj bos olamaz." });
            }

            var sessionId = ResolveSessionId(request.SessionId);
            try
            {
                var aiResult = await _aiService.ReplyAsync(request.Message.Trim(), sessionId, cancellationToken);
                var response = new ChatResponse
                {
                    Message = aiResult.Message,
                    Intent = aiResult.Intent,
                    Cars = aiResult.Cars,
                    SuggestedFilters = aiResult.SuggestedFilters
                };
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "Asistan su an yanit veremiyor. Lutfen tekrar deneyin." });
            }
        }

        private string ResolveSessionId(string? requested)
        {
            const string key = "ai-session-id";
            var fromSession = HttpContext.Session.GetString(key);
            if (!string.IsNullOrWhiteSpace(fromSession)) return fromSession;

            var newId = string.IsNullOrWhiteSpace(requested) ? Guid.NewGuid().ToString("N") : requested;
            HttpContext.Session.SetString(key, newId);
            return newId;
        }
    }
}
