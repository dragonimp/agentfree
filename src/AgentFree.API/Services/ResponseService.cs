using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Mvc;

namespace AgentFree.API.Services;

/// <summary>
/// 响应服务实现 — 核心业务逻辑
/// </summary>
public class ResponseService : IResponseService
{
    private readonly AppDbContext _ctx;
    private readonly IToolRegistry _toolRegistry;
    private readonly IMemoryManager _memoryManager;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<ResponseService> _logger;
    private readonly IChatClient _chatClient;

    // 缓存响应结果（实际项目应使用 Redis 等）
    private readonly Dictionary<string, ResponseOutput> _responseCache = new();

    public ResponseService(
        AppDbContext ctx,
        IToolRegistry toolRegistry,
        IMemoryManager memoryManager,
        IPromptBuilder promptBuilder,
        IChatClient chatClient,
        ILogger<ResponseService> logger)
    {
        _ctx = ctx;
        _toolRegistry = toolRegistry;
        _memoryManager = memoryManager;
        _promptBuilder = promptBuilder;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<ResponseOutput> ExecuteAsync(
        CreateResponseRequest request,
        CancellationToken ct = default)
    {
        var responseId = Guid.NewGuid().ToString("N");
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Creating response {ResponseId} for session {SessionId}",
            responseId, sessionId);

        // 获取 Agent 信息
        AgentInfo? agentInfo = null;
        if (!string.IsNullOrEmpty(request.AgentId))
        {
            var agent = await _ctx.Agents.FindAsync(int.Parse(request.AgentId));
            if (agent != null)
            {
                agentInfo = new AgentInfo
                {
                    Id = agent.Id.ToString(),
                    Name = agent.Name,
                    Description = agent.Description,
                    SystemPrompt = agent.Description ?? "",
                    ToolIds = new List<string>()
                };
            }
        }

        // 如果未找到指定 Agent，使用默认 Agent
        if (agentInfo == null)
        {
            var defaultAgent = await _ctx.Agents.FirstOrDefaultAsync(a => a.Name == "hermes-coding");
            if (defaultAgent != null)
            {
                agentInfo = new AgentInfo
                {
                    Id = defaultAgent.Id.ToString(),
                    Name = defaultAgent.Name,
                    Description = defaultAgent.Description,
                    SystemPrompt = defaultAgent.Description ?? "",
                    ToolIds = new List<string>()
                };
            }
        }

        // 如果仍没有 Agent，创建一个默认的
        agentInfo ??= new AgentInfo
        {
            Id = "0",
            Name = "default",
            SystemPrompt = "You are a helpful AI assistant.",
            ToolIds = new List<string>()
        };

        // 获取工具
        var tools = _toolRegistry.GetAll();

        // 构建消息
        var input = request.Input.FirstOrDefault() ?? new ResponseInputItem { Content = request.Config.SystemPrompt ?? "Hello" };
        var messages = _promptBuilder.BuildMessages(agentInfo, input.Content, new List<AgentFree.API.Core.ChatMessage>(), tools);

        // 执行 Agentic Loop
        var toolCalls = new List<ResponseToolCall>();
        var maxIterations = 10;
        IList<AgentFree.API.Core.ChatMessage> localMessages = messages;

        for (int i = 0; i < maxIterations; i++)
        {
            // Convert to MS.AI format
            var msMessages = localMessages.Select(m =>
                new Microsoft.Extensions.AI.ChatMessage(ParseRole(m.Role), m.Content)
            ).ToList();

            var chatOptions = new ChatOptions
            {
                MaxOutputTokens = request.Config.MaxOutputTokens ?? 2048,
                Temperature = request.Config.Temperature ?? 0.7f
            };

            // Register tools as AITools
            if (tools.Any())
            {
            var aiTools = tools
                .Select(t => Microsoft.Extensions.AI.AIFunctionFactory.Create(
                    (string args, CancellationToken ct) => ExecuteToolAsync(t, args),
                    t.Name,
                    t.Description,
                    null))
                .Cast<AITool>()
                .ToList();
                chatOptions.Tools = aiTools;
                chatOptions.ToolMode = ChatToolMode.Auto;
            }

            var responseChat = await _chatClient.GetResponseAsync(msMessages, chatOptions, ct);
            var assistantText = responseChat.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(assistantText))
            {
                assistantText = "[No response from model]";
            }

            // 检查工具调用
            var toolCallMatches = ParseToolCalls(assistantText);
            if (!toolCallMatches.Any())
            {
                // 完成响应
                var output = new ResponseOutput
                {
                    Id = responseId,
                    Status = "completed",
                    Output = new[] { new ResponseOutputItem
                    {
                        Type = "message",
                        Role = "assistant",
                        Content = assistantText
                    }},
                    CreatedAt = DateTime.UtcNow
                };

                _responseCache[responseId] = output;
                return output;
            }

            // 执行工具调用
            foreach (var toolCall in toolCallMatches)
            {
                var toolResult = await _toolRegistry.ExecuteAsync(toolCall.ToolId, toolCall.Arguments);
                var record = new ToolCallRecord
                {
                    ToolId = toolCall.ToolId,
                    Arguments = toolCall.Arguments,
                    Result = toolResult.Success
                        ? (toolResult.Data?.ToString() ?? "Success")
                        : (toolResult.Error ?? "Unknown error"),
                    Success = toolResult.Success
                };

                toolCalls.Add(new ResponseToolCall
                {
                    Id = toolCall.ToolId,
                    Name = _toolRegistry.GetById(toolCall.ToolId)?.Name ?? toolCall.ToolId,
                    Arguments = toolCall.Arguments,
                    Status = toolResult.Success ? "completed" : "failed"
                });

                _promptBuilder.AddToolResult(localMessages, record);
                localMessages.Add(new AgentFree.API.Core.ChatMessage
                {
                    Role = "assistant",
                    Content = assistantText
                });
            }
        }

        // 超时
        var timeoutOutput = new ResponseOutput
        {
            Id = responseId,
            Status = "completed",
            Output = new[] { new ResponseOutputItem
            {
                Type = "message",
                Role = "assistant",
                Content = "Max iterations reached. Please try again with a simpler request."
            }},
            CreatedAt = DateTime.UtcNow
        };

        _responseCache[responseId] = timeoutOutput;
        return timeoutOutput;
    }

    public async Task<ResponseOutput?> GetAsync(string id, CancellationToken ct = default)
    {
        _responseCache.TryGetValue(id, out var response);
        return response;
    }

    public async Task CancelAsync(string id, CancellationToken ct = default)
    {
        _responseCache.Remove(id);
        _logger.LogInformation("Response {Id} cancelled", id);
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<SseEvent> ExecuteStreamAsync(
        CreateResponseRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var responseId = Guid.NewGuid().ToString("N");
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

        // 发送 response created 事件
        yield return new SseEvent
        {
            Event = "response.created",
            Data = new { id = responseId, type = "response", status = "in_progress" }
        };

        // 执行非流式响应（流式需要额外实现）
        var response = await ExecuteAsync(request, ct);

        // 发送数据事件
        foreach (var item in response.Output)
        {
            if (item.Type == "message" && !string.IsNullOrEmpty(item.Content))
            {
                yield return new SseEvent
                {
                    Event = "response.completed",
                    Data = new { id = responseId, status = "completed", output = response.Output }
                };
            }
        }
    }

    // ==================== 辅助方法 ====================

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

    private static IEnumerable<(string ToolId, string Arguments)> ParseToolCalls(string text)
    {
        var results = new List<(string, string)>();
        var pattern = @"{{TOOL_CALL:([^:]+):(.+?)}}";
        var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var toolId = match.Groups[1].Value;
            var args = match.Groups[2].Value;
            results.Add((toolId, args));
        }

        return results;
    }

    private async Task<object?> ExecuteToolAsync(ITool tool, string arguments, System.Threading.CancellationToken ct = default)
    {
        var result = await tool.ExecuteAsync(arguments);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "Tool execution failed");
        return result.Data;
    }
}
