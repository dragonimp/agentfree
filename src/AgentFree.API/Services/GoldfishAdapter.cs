using AgentFree.API.Core;
using Microsoft.Extensions.AI;

namespace AgentFree.API.Services;

/// <summary>
/// Goldfish 适配器 — 对接 Goldfish AI 框架
/// </summary>
public class GoldfishAdapter : IAdapterService
{
    private readonly IChatClient _chatClient;
    private readonly AppDbContext _context;
    private readonly ILogger<GoldfishAdapter> _logger;

    public string AdapterType => "Goldfish";

    public GoldfishAdapter(
        IChatClient chatClient,
        AppDbContext context,
        ILogger<GoldfishAdapter> logger)
    {
        _chatClient = chatClient;
        _context = context;
        _logger = logger;
    }

    public async Task<AgentResponse> ChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default)
    {
        var history = await GetHistoryAsync(sessionId);
        var chatMessages = history.Select(m =>
            new ChatMessage(ParseRole(m.Role), m.Content)).ToList();

        chatMessages.Add(new ChatMessage(ChatRole.User, userMessage));

        var response = await _chatClient.GetResponseAsync(chatMessages, null, ct);
        var assistantContent = response.Text ?? string.Empty;

        // 保存到数据库
        await _context.Messages.AddRangeAsync(
            new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow },
            new Message { SessionId = sessionId, Role = "assistant", Content = assistantContent, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync(ct);

        return new AgentResponse
        {
            Content = assistantContent,
            Messages = new List<Core.ChatMessage> {
                new() { Role = "user", Content = userMessage },
                new() { Role = "assistant", Content = assistantContent }
            }
        };
    }

    public async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default)
    {
        // Save user message
        _context.Messages.Add(new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(ct);

        var history = await GetHistoryAsync(sessionId);
        var chatMessages = history.Select(m =>
            new ChatMessage(ParseRole(m.Role), m.Content)).ToList();

        chatMessages.Add(new ChatMessage(ChatRole.User, userMessage));

        string fullContent = string.Empty;
        await foreach (var update in _chatClient.GetStreamingResponseAsync(chatMessages, null, ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullContent += update.Text;
                yield return new StreamChunk { Delta = update.Text };
            }
        }

        // Save assistant message
        if (!string.IsNullOrEmpty(fullContent))
        {
            _context.Messages.Add(new Message { SessionId = sessionId, Role = "assistant", Content = fullContent, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync(ct);
        }

        yield return new StreamChunk { Done = true };
    }

    public async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId)
    {
        var msgs = await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return msgs.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList();
    }

    public Task CancelAsync(string sessionId)
    {
        _logger.LogInformation("Stream cancelled for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    private static ChatRole ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
    }
}
