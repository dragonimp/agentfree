using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using MsChatMessage = Microsoft.Extensions.AI.ChatMessage;
using CoreChatMessage = AgentFree.API.Core.ChatMessage;
using Microsoft.Extensions.Logging;

namespace AgentFree.API.Services;

/// <summary>
/// Goldfish 适配器 — 对接本地 LLM（Ollama / OpenAI）
/// 这是默认的适配器实现，通过 IChatClient 直接与 LLM 通信
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

    /// <summary>
    /// 标准模式对话
    /// </summary>
    public async Task<AgentResponse> ChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default)
    {
        var history = await GetHistoryAsync(sessionId);
        var msMessages = ConvertToMsMessages(history);
        msMessages.Add(new MsChatMessage(ChatRole.User, userMessage));

        var response = await _chatClient.GetResponseAsync(msMessages, null, ct);
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
            Messages = new List<CoreChatMessage> {
                new() { Role = "user", Content = userMessage },
                new() { Role = "assistant", Content = assistantContent }
            }
        };
    }

    /// <summary>
    /// 流式模式对话 — 返回 SSE 格式的响应
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 保存用户消息
        _context.Messages.Add(new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(ct);

        var history = await GetHistoryAsync(sessionId);
        var msMessages = ConvertToMsMessages(history);
        msMessages.Add(new MsChatMessage(ChatRole.User, userMessage));

        string fullContent = string.Empty;
        await foreach (var update in _chatClient.GetStreamingResponseAsync(msMessages, null, ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullContent += update.Text;
                yield return new StreamChunk { Delta = update.Text };
            }
        }

        // 保存助手消息
        if (!string.IsNullOrEmpty(fullContent))
        {
            _context.Messages.Add(new Message { SessionId = sessionId, Role = "assistant", Content = fullContent, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync(ct);
        }

        yield return new StreamChunk { Done = true };
    }

    /// <summary>
    /// 获取会话历史 — 返回 Core.ChatMessage 以匹配接口定义
    /// </summary>
    public async Task<IList<CoreChatMessage>> GetHistoryAsync(string sessionId)
    {
        var msgs = await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return msgs.Select(m => new CoreChatMessage 
        { 
            Role = m.Role, 
            Content = m.Content, 
            CreatedAt = m.CreatedAt 
        }).ToList();
    }

    /// <summary>
    /// 取消对话
    /// </summary>
    public Task CancelAsync(string sessionId)
    {
        _logger.LogInformation("Stream cancelled for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 将内部 ChatMessage 转换为 MS.AI ChatMessage
    /// </summary>
    private static List<MsChatMessage> ConvertToMsMessages(IList<CoreChatMessage> messages)
    {
        return messages.Select(m =>
            new MsChatMessage(ParseRole(m.Role), m.Content)).ToList();
    }

    /// <summary>
    /// 解析角色字符串为 ChatRole 枚举
    /// </summary>
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
