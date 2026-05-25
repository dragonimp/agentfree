using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AgentFree.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatClient chatClient,
            AppDbContext context,
            IConfiguration configuration,
            ILogger<ChatController> logger)
        {
            _chatClient = chatClient;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // GET /api/chat/models?agentId={id}
        [HttpGet("models")]
        public ActionResult<object> GetModels([FromQuery] int? agentId = null)
        {
            var provider = _configuration["LLM:Provider"] ?? "Ollama";
            var baseUrl = _configuration["LLM:BaseUrl"] ?? "http://localhost:11434";
            var model = _configuration["LLM:Model"] ?? "qwen2.5:7b";

            return Ok(new
            {
                provider = provider,
                model = model,
                baseUrl = provider == "OpenAI" ? null : baseUrl,
                models = new[] { model }
            });
        }

        // POST /api/chat/stream
        [HttpPost("stream")]
        public async Task StreamChat([FromBody] ChatRequestDto dto)
        {
            var sessionId = dto.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                var error = new { error = "sessionId is required" };
                HttpContext.Response.StatusCode = 400;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsync(JsonSerializer.Serialize(error));
                return;
            }

            // Ensure session exists, create if not
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                session = new Session
                {
                    Id = sessionId,
                    AgentId = dto.AgentId ?? 1,
                    Name = $"Chat {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (dto.AgentId.HasValue)
                    session.AgentId = dto.AgentId.Value;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Save user message
            var userMessage = new Message
            {
                SessionId = sessionId,
                Role = dto.Role ?? "user",
                Content = dto.Content ?? string.Empty,
                ToolCallId = dto.ToolCallId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(userMessage);
            await _context.SaveChangesAsync();

            // Load conversation history
            var history = await _context.Messages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Convert to ChatMessage for IChatClient
            var chatMessages = new List<ChatMessage>();
            foreach (var msg in history)
            {
                var role = msg.Role.ToLowerInvariant();
                if (role == "system")
                    chatMessages.Add(new ChatMessage(ChatRole.System, msg.Content));
                else if (role == "user")
                    chatMessages.Add(new ChatMessage(ChatRole.User, msg.Content));
                else if (role == "assistant")
                    chatMessages.Add(new ChatMessage(ChatRole.Assistant, msg.Content));
                else if (role == "tool")
                    chatMessages.Add(new ChatMessage(ChatRole.Tool, msg.Content));
            }

            var chatOptions = new ChatOptions();

            // Stream response via SSE
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers.CacheControl = "no-cache";
            HttpContext.Response.Headers.Connection = "keep-alive";

            var sw = new StreamWriter(HttpContext.Response.Body, Encoding.UTF8);

            string fullAssistantContent = string.Empty;

            await foreach (var update in _chatClient.GetStreamingResponseAsync(chatMessages, chatOptions, HttpContext.RequestAborted))
            {
                var text = update.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    fullAssistantContent += text;

                    var data = new { delta = text };
                    var json = JsonSerializer.Serialize(data);
                    await sw.WriteAsync($"data: {json}\n\n");
                    await sw.FlushAsync();
                }
            }

            // Done signal
            var doneData = new { done = true };
            var doneJson = JsonSerializer.Serialize(doneData);
            await sw.WriteAsync($"data: {doneJson}\n\n");
            await sw.FlushAsync();

            // Save assistant response to database
            if (!string.IsNullOrEmpty(fullAssistantContent))
            {
                var assistantMessage = new Message
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = fullAssistantContent,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(assistantMessage);
                await _context.SaveChangesAsync();
            }

            await sw.FlushAsync();
        }
    }

    public class ChatRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int? AgentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
        public string? ToolCallId { get; set; }
    }
}
