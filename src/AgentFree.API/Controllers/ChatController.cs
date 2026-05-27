using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models;
using AgentFree.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AgentFree.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IAdapterRouter _adapterRouter;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IAdapterRouter adapterRouter,
            AppDbContext context,
            IConfiguration configuration,
            ILogger<ChatController> logger)
        {
            _adapterRouter = adapterRouter;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // GET /api/chat/adapters — 列出可用适配器
        [HttpGet("adapters")]
        public IActionResult GetAdapters()
        {
            var adapters = _adapterRouter.GetAvailableAdapters();
            return Ok(adapters);
        }

        // GET /api/chat/models?agentId={id}
        [HttpGet("models")]
        public ActionResult<object> GetModels([FromQuery] int? agentId = null)
        {
            if (agentId.HasValue)
            {
                var agent = _context.Agents.FindAsync(agentId.Value).Result;
                if (agent != null)
                {
                    // 对话大模型类型的自定义配置
                    if (agent.AgentType == "对话大模型")
                    {
                        return Ok(new
                        {
                            provider = agent.LLMProvider ?? "OpenAI",
                            model = agent.LLMModelName ?? "gpt-4o",
                            baseUrl = agent.LLMBaseUrl ?? "https://api.openai.com",
                            models = new[] { agent.LLMModelName ?? "gpt-4o" }
                        });
                    }
                }
            }

            var model = _configuration["LLM:Model"] ?? "gpt-4o";

            return Ok(new
            {
                provider = "OpenAI",
                model = model,
                baseUrl = "https://api.openai.com",
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

            // 加载 Agent 信息
            AgentInfo? agentInfo = null;
            if (dto.AgentId.HasValue)
            {
                var agent = await _context.Agents.FindAsync(dto.AgentId.Value);
                if (agent != null)
                {
                    agentInfo = new AgentInfo
                    {
                        Name = agent.Name,
                        AgentType = agent.AgentType,
                        SystemPrompt = agent.SystemPrompt
                    };
                    // 填充 ExtraData（对话大模型的 LLM 配置）
                    if (!string.IsNullOrEmpty(agent.LLMProvider))
                        agentInfo.ExtraData["LLMProvider"] = agent.LLMProvider;
                    if (!string.IsNullOrEmpty(agent.LLMBaseUrl))
                        agentInfo.ExtraData["LLMBaseUrl"] = agent.LLMBaseUrl;
                    if (!string.IsNullOrEmpty(agent.LLMModelName))
                        agentInfo.ExtraData["LLMModelName"] = agent.LLMModelName;
                    if (!string.IsNullOrEmpty(agent.LLMApiKey))
                        agentInfo.ExtraData["LLMApiKey"] = agent.LLMApiKey;
                    // 填充 ExtraData（Hermes 网关配置）
                    if (!string.IsNullOrEmpty(agent.ServiceUrl))
                        agentInfo.ExtraData["HermesBaseUrl"] = agent.ServiceUrl;
                    if (!string.IsNullOrEmpty(agent.Token))
                        agentInfo.ExtraData["HermesApiKey"] = agent.Token;
                }
            }

            var agentType = agentInfo?.AgentType ?? "Goldfish";

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

            // 保存用户消息
            var userMessage = dto.Content ?? string.Empty;
            _context.Messages.Add(new Message
            {
                SessionId = sessionId,
                Role = dto.Role ?? "user",
                Content = userMessage,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // 选择适配器
            IAdapterService adapter;
            try
            {
                adapter = _adapterRouter.GetAdapter(agentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get adapter for type {AgentType}", agentType);
                var errorData = new { error = $"适配器路由失败: {ex.Message}" };
                HttpContext.Response.StatusCode = 500;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsync(JsonSerializer.Serialize(errorData));
                return;
            }

            // SSE 流式响应
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers.CacheControl = "no-cache";
            HttpContext.Response.Headers.Connection = "keep-alive";

            var sw = new StreamWriter(HttpContext.Response.Body, Encoding.UTF8);
            string fullAssistantContent = string.Empty;

            try
            {
                _logger.LogInformation("Streaming chat via {AdapterType} for session {SessionId}", agentType, sessionId);

                await foreach (var chunk in adapter.StreamChatAsync(agentInfo, sessionId, userMessage, HttpContext.RequestAborted))
                {
                    if (!string.IsNullOrEmpty(chunk.Delta))
                    {
                        fullAssistantContent += chunk.Delta;
                        var data = new { delta = chunk.Delta };
                        var json = JsonSerializer.Serialize(data);
                        await sw.WriteAsync($"data: {json}\n\n");
                        await sw.FlushAsync();
                    }

                    if (chunk.Done)
                    {
                        var doneData = new { done = true };
                        var doneJson = JsonSerializer.Serialize(doneData);
                        await sw.WriteAsync($"data: {doneJson}\n\n");
                        await sw.FlushAsync();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StreamChat error for session {SessionId} via {AdapterType}", sessionId, agentType);

                var errorData = new { error = "服务响应失败: " + ex.Message, detail = ex.InnerException?.Message ?? ex.Message, adapter = agentType };
                var errorJson = JsonSerializer.Serialize(errorData);
                await sw.WriteAsync($"data: {errorJson}\n\n");
                await sw.FlushAsync();

                HttpContext.Response.StatusCode = 502;
            }
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
