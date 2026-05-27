using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AgentFree.API.Services;

/// <summary>
/// Hermes 适配器 — 对接 Hermes API Server
/// 通过 HTTP 调用 Hermes 的对话接口，实现跨框架智能体通信
/// </summary>
public class HermesAdapter : IAdapterService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HermesAdapter> _logger;

    public string AdapterType => "Hermes";

    public HermesAdapter(
        IHttpClientFactory httpClientFactory,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<HermesAdapter> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Hermes");
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 标准模式对话 — 通过 Hermes API Server 调用智能体
    /// </summary>
    public async Task<AgentResponse> ChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default)
    {
        // 优先从 agentInfo.ExtraData 读取，其次从配置读取
        var hermesBaseUrl = agentInfo?.ExtraData.GetValueOrDefault("HermesBaseUrl")
            ?? _configuration["Hermes:BaseUrl"] ?? "http://localhost:5200";
        var hermesApiKey = agentInfo?.ExtraData.GetValueOrDefault("HermesApiKey");
        _httpClient.BaseAddress = new Uri(hermesBaseUrl);
        // 设置 API Key 头
        if (!string.IsNullOrEmpty(hermesApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", hermesApiKey);
        }

        // 加载历史
        var history = await GetHistoryAsync(sessionId);
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        messages.Add(new { role = "user", content = userMessage });

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/hermes/chat",
                new { sessionId, messages, agentName = agentInfo?.Name },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Hermes API returned error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return new AgentResponse
                {
                    Content = $"[Hermes 适配器错误] HTTP {response.StatusCode}",
                    Messages = new List<Core.ChatMessage>
                    {
                        new() { Role = "user", Content = userMessage },
                        new() { Role = "assistant", Content = $"[Hermes 适配器错误] HTTP {response.StatusCode}" }
                    }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<HermesChatResponse>(cancellationToken: ct);

            var assistantContent = result?.Content ?? string.Empty;

            // 保存到数据库
            await _context.Messages.AddRangeAsync(
                new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow },
                new Message { SessionId = sessionId, Role = "assistant", Content = assistantContent, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync(ct);

            return new AgentResponse
            {
                Content = assistantContent,
                Messages = new List<Core.ChatMessage>
                {
                    new() { Role = "user", Content = userMessage },
                    new() { Role = "assistant", Content = assistantContent }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hermes adapter chat failed");
            return new AgentResponse
            {
                Content = $"[Hermes 适配器错误] {ex.Message}",
                Messages = new List<Core.ChatMessage>
                {
                    new() { Role = "user", Content = userMessage },
                    new() { Role = "assistant", Content = $"[Hermes 适配器错误] {ex.Message}" }
                }
            };
        }
    }

    /// <summary>
    /// 流式模式对话 — 通过 Hermes API Server 获取流式响应
    /// 注意：C# 不允许在 try-catch 中使用 yield return，所以收集结果后统一 yield
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> StreamChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 优先从 agentInfo.ExtraData 读取，其次从配置读取
        var hermesBaseUrl = agentInfo?.ExtraData.GetValueOrDefault("HermesBaseUrl")
            ?? _configuration["Hermes:BaseUrl"] ?? "http://localhost:5200";
        var hermesApiKey = agentInfo?.ExtraData.GetValueOrDefault("HermesApiKey");
        _httpClient.BaseAddress = new Uri(hermesBaseUrl);
        // 设置 API Key 头
        if (!string.IsNullOrEmpty(hermesApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", hermesApiKey);
        }

        // 保存用户消息
        _context.Messages.Add(new Message { SessionId = sessionId, Role = "user", Content = userMessage, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(ct);

        var history = await GetHistoryAsync(sessionId);
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        messages.Add(new { role = "user", content = userMessage });

        // 预构建请求内容
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            sessionId,
            messages,
            agentName = agentInfo?.Name
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        var resultChunks = new List<StreamChunk>();
        string? errorDelta = null;

        try
        {
            using var response = await _httpClient.PostAsync(
                "/api/hermes/chat/stream",
                content,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                errorDelta = $"[Hermes 适配器错误] HTTP {response.StatusCode}";
            }
            else
            {
                var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new System.IO.StreamReader(stream);

                string line;
                string fullContent = string.Empty;
                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    if (line.StartsWith("data: "))
                    {
                        var jsonStr = line.Substring(6);
                        var data = System.Text.Json.JsonSerializer.Deserialize<HermesStreamChunk>(jsonStr);
                        if (data?.Delta != null)
                        {
                            fullContent += data.Delta;
                            resultChunks.Add(new StreamChunk { Delta = data.Delta });
                        }
                        if (data?.Done == true)
                        {
                            // 保存助手消息
                            if (!string.IsNullOrEmpty(fullContent))
                            {
                                _context.Messages.Add(new Message { SessionId = sessionId, Role = "assistant", Content = fullContent, CreatedAt = DateTime.UtcNow });
                                await _context.SaveChangesAsync(ct);
                            }
                            resultChunks.Add(new StreamChunk { Done = true });
                            break;
                        }
                    }
                }

                // 正常结束（无 done 信号）
                if (!string.IsNullOrEmpty(fullContent) && !resultChunks.Any(c => c.Done))
                {
                    _context.Messages.Add(new Message { SessionId = sessionId, Role = "assistant", Content = fullContent, CreatedAt = DateTime.UtcNow });
                    await _context.SaveChangesAsync(ct);
                }
                if (!resultChunks.Any(c => c.Done))
                {
                    resultChunks.Add(new StreamChunk { Done = true });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hermes adapter stream chat failed");
            errorDelta = $"[Hermes 适配器错误] {ex.Message}";
        }

        // 在 try-catch 外面 yield return — 这是允许的
        if (errorDelta != null)
        {
            yield return new StreamChunk { Delta = errorDelta, Done = true };
        }
        else
        {
            foreach (var chunk in resultChunks)
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    public async Task<IList<Core.ChatMessage>> GetHistoryAsync(string sessionId)
    {
        var msgs = await _context.Messages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return msgs.Select(m => new Core.ChatMessage
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
}

/// <summary>
/// Hermes API 响应模型
/// </summary>
public class HermesChatResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public string? Content { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Hermes 流式响应块
/// </summary>
public class HermesStreamChunk
{
    [System.Text.Json.Serialization.JsonPropertyName("delta")]
    public string? Delta { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("done")]
    public bool? Done { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
