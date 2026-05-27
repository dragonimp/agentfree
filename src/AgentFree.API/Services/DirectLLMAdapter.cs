using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using CoreChatMessage = AgentFree.API.Core.ChatMessage;
using MsChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AgentFree.API.Services;

/// <summary>
/// DirectLLM 适配器 — 直接对接原始大模型
/// 支持 OpenAI、Ollama、Azure 等多种 LLM 提供商
/// 用户可自定义配置 provider、baseUrl、model、apiKey
/// </summary>
public class DirectLLMAdapter : IAdapterService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DirectLLMAdapter> _logger;

    public string AdapterType => "对话大模型";

    public DirectLLMAdapter(
        AppDbContext context,
        ILogger<DirectLLMAdapter> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 创建 IChatClient — 基于 OpenAI 兼容接口
    /// </summary>
    private IChatClient CreateChatClient(AgentInfo? agentInfo)
    {
        // 从 Agent 的 ExtraData 字典中读取配置
        var extraData = agentInfo?.ExtraData as Dictionary<string, string> ?? new Dictionary<string, string>();
        var llmBaseUrl = extraData.GetValueOrDefault("LLMBaseUrl") ?? "https://api.openai.com";
        var llmModel = extraData.GetValueOrDefault("LLMModelName") ?? "gpt-4o";
        var llmApiKey = extraData.GetValueOrDefault("LLMApiKey") ?? string.Empty;

        _logger.LogInformation("DirectLLM 适配器: Model={Model}, BaseUrl={BaseUrl}",
            llmModel, llmBaseUrl);

        return CreateOpenAIClient(llmBaseUrl, llmModel, llmApiKey);
    }

    private IChatClient CreateOpenAIClient(string baseUrl, string modelName, string? apiKey)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl)
        };
        var openaiClient = new OpenAIClient(apiKey ?? "", options);
        return openaiClient.GetChatClient(modelName).AsIChatClient();
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
        using var chatClient = CreateChatClient(agentInfo);

        var history = await GetHistoryAsync(sessionId);
        var msMessages = ConvertToMsMessages(history);
        msMessages.Add(new MsChatMessage(ChatRole.User, userMessage));

        var response = await chatClient.GetResponseAsync(msMessages, null, ct);
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
    /// 流式模式对话
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var chatClient = CreateChatClient(agentInfo);

        // 保存用户消息
        _context.Messages.Add(new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(ct);

        var history = await GetHistoryAsync(sessionId);
        var msMessages = ConvertToMsMessages(history);
        msMessages.Add(new MsChatMessage(ChatRole.User, userMessage));

        string fullContent = string.Empty;
        await foreach (var update in chatClient.GetStreamingResponseAsync(msMessages, null, ct))
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
    /// 获取会话历史
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

    private static List<MsChatMessage> ConvertToMsMessages(IList<CoreChatMessage> messages)
    {
        return messages.Select(m =>
            new MsChatMessage(ParseRole(m.Role), m.Content)).ToList();
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
