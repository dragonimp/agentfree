# AgentFree（Goldfish）架构设计文档

> **项目代号**: Goldfish  
> **版本**: v1.0  
> **日期**: 2026-05-23  
> **状态**: 设计完成，进入编码阶段

---

## 1. 概述

### 1.1 项目定位

AgentFree 是一个基于 .NET 10 的智能体运行平台，提供：
- **统一的 LLM 调用抽象**（多提供商支持）
- **Agentic Loop 引擎**（工具调用 → 执行 → 反馈循环）
- **工具发现与编排**（自动发现 + 手动注册）
- **会话记忆管理**（短期 + 长期记忆）
- **管理后台**（React SPA + .NET 10 API）
- **OpenAI Responses 协议兼容**（标准 API 接口）
- **协议适配器层**（MCP / A2UI / AG-UI / Skills）

### 1.2 技术决策

| 决策点 | 选择 | 理由 |
|--------|------|------|
| LLM 统一抽象 | **Microsoft.Extensions.AI** | 官方标准，支持多提供商，无需重复造轮子 |
| Agentic Loop | **自实现** | 轻量 + 可控 + 特定场景需求 |
| 工具机制 | **自实现** | 需要集成到 .NET DI 生态，与 MS Agent Framework 对齐 |
| 记忆系统 | **自实现** | 集成到现有 PostgreSQL 架构 |
| API 协议 | **OpenAI Responses** | 行业标准，生态兼容 |
| 前端 | React + TypeScript + Vite | 现有架构，bigdata 风格 |

---

## 2. 架构设计

### 2.1 总体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        AgentFree Platform                        │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    Frontend (React SPA)                   │   │
│  │  Home │ Agent Management │ Tool Registry │ Logs          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   API Layer (OpenAI Responses)            │   │
│  │  ┌─────────────────────────────────────────────────────┐  │   │
│  │  │ POST /v1/responses (创建响应)                       │  │   │
│  │  │ GET /v1/responses/{id} (获取响应)                   │  │   │
│  │  │ POST /v1/responses/{id}/cancel (取消响应)           │  │   │
│  │  └─────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │           Protocol Adapter Layer (协议适配层)             │   │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────┐      │   │
│  │  │ MCP     │ │ A2UI    │ │ AG-UI   │ │ Skills   │      │   │
│  │  │ Adapter │ │ Adapter │ │ Adapter │ │ Adapter  │      │   │
│  │  └─────────┘ └─────────┘ └─────────┘ └──────────┘      │   │
│  │       │           │           │           │               │   │
│  │       └───────────┴───────────┴───────────┘               │   │
│  │                        │                                  │   │
│  │              ┌─────────────────────┐                      │   │
│  │              │   IProtocolAdapter  │                      │   │
│  │              │   (统一接口)          │                      │   │
│  │              └─────────────────────┘                      │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                Core Engine (Agentic Loop)                 │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌───────────────────┐  │   │
│  │  │ IChatClient │ │ ToolEngine  │ │ Memory Manager    │  │   │
│  │  │ (MS 底层)    │ │ 工具执行    │ │ 会话/长期记忆     │  │   │
│  │  └─────────────┘ └─────────────┘ └───────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                Data Layer (PostgreSQL)                    │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌───────────────────┐  │   │
│  │  │ Agents      │ │ Tools       │ │ Sessions          │  │   │
│  │  │ 智能体表     │ │ 工具表      │ │ 会话表            │  │   │
│  │  └─────────────┘ └─────────────┘ └───────────────────┘  │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 核心交互流程

```
用户输入
   │
   ▼
┌─────────────────┐
│  Agentic Loop   │◄── 工具调用循环 ───┐
│                 │                    │
│ 1. 构建消息上下文 │                    │
│ 2. 调用 IChatClient │                  │
│ 3. 检查响应      │                    │
│    ├─ 文本回复 → 返回结果              │
│    └─ 工具调用 → 执行工具 ─────────────┘
│                                      │
└─────────────────┘
   │
   ▼
前端展示 / API 返回
```

---

## 3. 核心接口设计

### 3.1 IAgent (智能体抽象)

```csharp
/// <summary>
/// 智能体核心接口 — 封装完整的 agentic loop
/// </summary>
public interface IAgent
{
    /// <summary>
    /// 智能体标识
    /// </summary>
    AgentInfo Identity { get; }

    /// <summary>
    /// 执行智能体任务
    /// </summary>
    /// <param name="prompt">用户输入/任务描述</param>
    /// <param name="context">上下文信息（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>智能体的完整响应（含工具调用历史）</returns>
    Task<AgentResponse> ExecuteAsync(
        string prompt, 
        AgentContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话历史
    /// </summary>
    Task<IList<ChatMessage>> GetHistoryAsync(string sessionId);

    /// <summary>
    /// 重置会话
    /// </summary>
    Task ResetSessionAsync(string sessionId);
}

/// <summary>
/// 智能体信息
/// </summary>
public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public ICollection<string> ToolIds { get; set; } = new List<string>();
}

/// <summary>
/// 智能体上下文
/// </summary>
public class AgentContext
{
    public string? SessionId { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// 智能体响应
/// </summary>
public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public List<ToolCallRecord> ToolCalls { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}

/// <summary>
/// 工具调用记录
/// </summary>
public class ToolCallRecord
{
    public string ToolId { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
}
```

### 3.2 IToolRegistry (工具注册表)

```csharp
/// <summary>
/// 工具注册表 — 负责工具的发现、注册和执行
/// 参考 AIFunctionFactory 的设计
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// 注册一个工具
    /// </summary>
    void Register(ITool tool);

    /// <summary>
    /// 注册一组工具
    /// </summary>
    void RegisterRange(IEnumerable<ITool> tools);

    /// <summary>
    /// 从程序集自动发现并注册工具
    /// </summary>
    void DiscoverFromAssembly(Assembly assembly);

    /// <summary>
    /// 获取已注册的工具列表
    /// </summary>
    IList<ITool> GetAll();

    /// <summary>
    /// 根据 ID 获取工具
    /// </summary>
    ITool? GetById(string toolId);

    /// <summary>
    /// 执行工具调用
    /// </summary>
    Task<ToolResult> ExecuteAsync(string toolId, string arguments);
}

/// <summary>
/// 工具接口
/// </summary>
public interface ITool
{
    /// <summary>
    /// 工具唯一标识
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 工具名称（用于 LLM 调用）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述（用于 LLM 理解）
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 工具参数 Schema（JSON Schema 格式）
    /// </summary>
    string ParametersSchema { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    Task<ToolResult> ExecuteAsync(string arguments);

    /// <summary>
    /// 工具是否可用（依赖检查）
    /// </summary>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}
```

### 3.3 IMemoryManager (记忆管理器)

```csharp
/// <summary>
/// 记忆管理器 — 管理智能体的短期和长期记忆
/// </summary>
public interface IMemoryManager
{
    /// <summary>
    /// 添加消息到会话历史（短期记忆）
    /// </summary>
    Task AddMessageAsync(string sessionId, ChatMessage message);

    /// <summary>
    /// 获取会话历史
    /// </summary>
    Task<IList<ChatMessage>> GetHistoryAsync(string sessionId);

    /// <summary>
    /// 添加长期记忆
    /// </summary>
    Task AddMemoryAsync(MemoryEntry entry);

    /// <summary>
    /// 搜索长期记忆（基于语义相似度）
    /// </summary>
    Task<IList<MemoryEntry>> SearchAsync(string query, int limit = 5);

    /// <summary>
    /// 删除会话
    /// </summary>
    Task DeleteSessionAsync(string sessionId);

    /// <summary>
    /// 压缩会话历史（减少 Token 使用）
    /// </summary>
    Task<IList<ChatMessage>> CompressAsync(string sessionId);
}

/// <summary>
/// 记忆条目
/// </summary>
public class MemoryEntry
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "General"; // General/UserPreference/Fact
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public IList<float>? Embedding { get; set; } // 向量嵌入
}
```

### 3.4 IPromptBuilder (提示词构建器)

```csharp
/// <summary>
/// 提示词构建器 — 构建发送给 LLM 的完整提示词
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// 构建系统提示词
    /// </summary>
    string BuildSystemPrompt(AgentInfo agent);

    /// <summary>
    /// 构建完整消息列表
    /// </summary>
    IList<ChatMessage> BuildMessages(
        AgentInfo agent, 
        string userPrompt,
        IList<ChatMessage> history,
        IList<ITool> tools);

    /// <summary>
    /// 添加工具调用结果到消息列表
    /// </summary>
    void AddToolResult(IList<ChatMessage> messages, ToolCallRecord record);
}
```

---

## 4. OpenAI Responses API 兼容层

### 4.1 请求格式

```csharp
// OpenAI Responses API 请求格式
public class CreateResponseRequest
{
    /// <summary>
    /// 智能体标识（可选，不传则使用默认智能体）
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// 输入内容（支持文本、文件、图片等）
    /// </summary>
    public IEnumerable<ResponseInputItem> Input { get; set; } = Array.Empty<ResponseInputItem>();

    /// <summary>
    /// 智能体配置（模型、工具、参数等）
    /// </summary>
    public ResponseConfig Config { get; set; } = new();

    /// <summary>
    /// 是否流式输出（默认false）
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// 会话ID（用于多轮对话）
    /// </summary>
    public string? SessionId { get; set; }
}

// 输入项
public class ResponseInputItem
{
    public string Type { get; set; } = "message"; // message/file
    public string Role { get; set; } = "user"; // user/assistant
    public string Content { get; set; } = string.Empty;
    public string? FileId { get; set; } // 文件类型时
}

// 配置
public class ResponseConfig
{
    /// <summary>
    /// 模型标识（可选，覆盖默认模型）
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 系统提示词（可选，覆盖默认提示词）
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 工具列表（可选，覆盖默认工具集）
    /// </summary>
    public IEnumerable<ResponseTool> Tools { get; set; } = Array.Empty<ResponseTool>();

    /// <summary>
    /// 响应格式
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// 停止词
    /// </summary>
    public IEnumerable<string>? StopSequences { get; set; }

    /// <summary>
    /// 温度参数
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// 最大输出Token
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 是否启用深度思考
    /// </summary>
    public bool? Reasoning { get; set; }
}

// 工具定义（与OpenAI格式兼容）
public class ResponseTool
{
    public string Type { get; set; } = "function";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersSchema { get; set; } = "{}"; // JSON Schema
    public bool? Strict { get; set; }
}
```

### 4.2 响应格式

```csharp
// 标准响应
public class ResponseOutput
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "response";
    public string Status { get; set; } = "completed"; // completed/in_progress/error
    public IEnumerable<ResponseOutputItem> Output { get; set; } = Array.Empty<ResponseOutputItem>();
    public ResponseUsage? Usage { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// 输出项
public class ResponseOutputItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "message"; // message/tool_call
    public string Status { get; set; } = "completed";
    public string? Role { get; set; } // assistant
    public string? Content { get; set; }
    public ResponseToolCall? ToolCall { get; set; }
}

// 工具调用
public class ResponseToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "function_call";
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty; // JSON字符串
    public string? Status { get; set; }
}

// 使用情况
public class ResponseUsage
{
    public int TotalTokens { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
```

### 4.3 API端点设计

```csharp
// 创建响应（兼容OpenAI Responses API）
[ApiController]
[Route("v1/[controller]")]
public class ResponsesController : ControllerBase
{
    private readonly IResponseService _responseService;
    private readonly ILogger<ResponsesController> _logger;

    public ResponsesController(
        IResponseService responseService,
        ILogger<ResponsesController> logger)
    {
        _responseService = responseService;
        _logger = logger;
    }

    // POST /v1/responses - 创建新响应
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateResponseRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Stream)
            {
                // 流式响应 - 返回SSE格式
                return StreamingResults.SseEventStream(
                    _responseService.ExecuteStreamAsync(request, ct));
            }
            else
            {
                // 标准响应
                var response = await _responseService.ExecuteAsync(request, ct);
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response creation failed");
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }

    // GET /v1/responses/{id} - 获取响应详情
    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken ct)
    {
        try
        {
            var response = await _responseService.GetAsync(id, ct);
            if (response == null)
                return NotFound(new { error = new { message = "Response not found" } });
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response retrieval failed: {Id}", id);
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }

    // POST /v1/responses/{id}/cancel - 取消响应
    [HttpPost("{id}/cancel")]
    [Produces("application/json")]
    public async Task<IActionResult> CancelAsync(string id, CancellationToken ct)
    {
        try
        {
            await _responseService.CancelAsync(id, ct);
            return Ok(new { id, status = "cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Response cancellation failed: {Id}", id);
            return StatusCode(500, new
            {
                error = new
                {
                    message = ex.Message,
                    type = "internal_error",
                    code = 500
                }
            });
        }
    }
}
```

---

## 5. 协议适配器层设计

### 5.1 协议适配器定位

```
┌─────────────────────────────────────────────────────────────┐
│                     AgentFree Platform                      │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                   API Layer                          │   │
│  │  OpenAI Responses API (标准协议)                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │             Protocol Adapters Layer                   │   │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────┐  │   │
│  │  │ MCP     │ │ A2UI    │ │ AG-UI   │ │ Skills   │  │   │
│  │  │ Adapter │ │ Adapter │ │ Adapter │ │ Adapter  │  │   │
│  │  └─────────┘ └─────────┘ └─────────┘ └──────────┘  │   │
│  │       │           │           │           │           │   │
│  │       └───────────┴───────────┴───────────┘           │   │
│  │                        │                              │   │
│  │              ┌─────────────────────┐                  │   │
│  │              │   IProtocolAdapter  │                  │   │
│  │              │   (统一接口)          │                  │   │
│  │              └─────────────────────┘                  │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                Core Engine (Agentic Loop)             │   │
│  │  IAgent | IChatClient | ToolRegistry | Memory       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 协议适配器统一接口

```csharp
/// <summary>
/// 协议适配器接口 — 所有协议实现必须遵循此契约
/// </summary>
public interface IProtocolAdapter
{
    /// <summary>
    /// 协议标识（用于识别和路由）
    /// </summary>
    string ProtocolId { get; }

    /// <summary>
    /// 协议名称（用于显示）
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// 协议版本
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 将协议消息转换为标准消息格式
    /// </summary>
    Task<IList<ChatMessage>> TranslateToStandardAsync(ProtocolMessage message);

    /// <summary>
    /// 将标准消息格式转换为协议消息
    /// </summary>
    Task<ProtocolMessage> TranslateToProtocolAsync(IList<ChatMessage> messages);

    /// <summary>
    /// 验证协议消息是否有效
    /// </summary>
    Task<bool> ValidateAsync(ProtocolMessage message);

    /// <summary>
    /// 获取协议所需的依赖项
    /// </summary>
    Task<ProtocolDependencies> GetDependenciesAsync();

    /// <summary>
    /// 协议是否可用
    /// </summary>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// 协议消息（统一中间格式）
/// </summary>
public class ProtocolMessage
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // message/tool_call/tool_result
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 协议依赖项
/// </summary>
public class ProtocolDependencies
{
    public bool RequiresInternet { get; set; }
    public bool RequiresLocalServer { get; set; }
    public IEnumerable<string> RequiredPackages { get; set; } = Array.Empty<string>();
    public IEnumerable<string> EnvironmentVariables { get; set; } = Array.Empty<string>();
}
```

### 5.3 协议适配器注册与路由

```csharp
/// <summary>
/// 协议管理器 — 负责注册、发现和路由
/// </summary>
public interface IProtocolManager
{
    /// <summary>
    /// 注册协议适配器
    /// </summary>
    void Register(IProtocolAdapter adapter);

    /// <summary>
    /// 根据协议ID获取适配器
    /// </summary>
    IProtocolAdapter? GetByProtocolId(string protocolId);

    /// <summary>
    /// 获取所有已注册的适配器
    /// </summary>
    IEnumerable<IProtocolAdapter> GetAll();

    /// <summary>
    /// 将协议消息转换为标准消息
    /// </summary>
    Task<IList<ChatMessage>> TranslateAsync(string protocolId, ProtocolMessage message);

    /// <summary>
    /// 将标准消息转换为协议消息
    /// </summary>
    Task<ProtocolMessage> TranslateBackAsync(string protocolId, IList<ChatMessage> messages);
}

/// <summary>
/// 协议管理器实现
/// </summary>
public class ProtocolManager : IProtocolManager
{
    private readonly Dictionary<string, IProtocolAdapter> _adapters = new();

    public void Register(IProtocolAdapter adapter)
    {
        _adapters[adapter.ProtocolId] = adapter;
    }

    public IProtocolAdapter? GetByProtocolId(string protocolId)
        => _adapters.TryGetValue(protocolId, out var adapter) ? adapter : null;

    public IEnumerable<IProtocolAdapter> GetAll() => _adapters.Values;

    public async Task<IList<ChatMessage>> TranslateAsync(
        string protocolId, 
        ProtocolMessage message)
    {
        if (!_adapters.TryGetValue(protocolId, out var adapter))
            throw new InvalidOperationException($"Protocol not found: {protocolId}");

        return await adapter.TranslateToStandardAsync(message);
    }

    public async Task<ProtocolMessage> TranslateBackAsync(
        string protocolId, 
        IList<ChatMessage> messages)
    {
        if (!_adapters.TryGetValue(protocolId, out var adapter))
            throw new InvalidOperationException($"Protocol not found: {protocolId}");

        return await adapter.TranslateToProtocolAsync(messages);
    }
}
```

---

## 6. 数据库设计

### 6.1 Agent 表（智能体）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | int | PK, 自增 | 主键 |
| Name | nvarchar(100) | NOT NULL, 唯一 | 智能体名称 |
| Description | nvarchar(500) | | 描述 |
| SystemPrompt | nvarchar(max) | | 系统提示词 |
| CreatedAt | datetime2 | DEFAULT | 创建时间 |
| UpdatedAt | datetime2 | DEFAULT | 更新时间 |

**注意**：移除 `ProtocolType` 字段，协议适配器是内部实现，不是用户配置。

### 6.2 Session 表（会话）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | nvarchar(36) | PK | 会话 ID（GUID） |
| AgentId | int | FK | 关联智能体 |
| Name | nvarchar(100) | | 会话名称 |
| CreatedAt | datetime2 | DEFAULT | 创建时间 |
| UpdatedAt | datetime2 | DEFAULT | 更新时间 |

### 6.3 Message 表（消息）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | bigint | PK, 自增 | 主键 |
| SessionId | nvarchar(36) | FK | 关联会话 |
| Role | nvarchar(20) | NOT NULL | user/assistant/tool/system |
| Content | nvarchar(max) | | 消息内容 |
| ToolCallId | nvarchar(50) | | 工具调用 ID（可选） |
| CreatedAt | datetime2 | DEFAULT | 创建时间 |

### 6.4 Tool 表（工具）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | nvarchar(50) | PK | 工具 ID |
| Name | nvarchar(100) | NOT NULL | 工具名称 |
| Description | nvarchar(500) | NOT NULL | 工具描述 |
| ParametersSchema | nvarchar(max) | | JSON Schema |
| IsActive | bit | DEFAULT 1 | 是否启用 |
| CreatedAt | datetime2 | DEFAULT | 创建时间 |

### 6.5 Memory 表（长期记忆）

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | nvarchar(36) | PK | 记忆 ID |
| Content | nvarchar(max) | NOT NULL | 记忆内容 |
| Type | nvarchar(50) | DEFAULT 'General' | 记忆类型 |
| Category | nvarchar(50) | | 分类 |
| Embedding | vector(1536) | | 向量嵌入 |
| CreatedAt | datetime2 | DEFAULT | 创建时间 |

---

## 7. 模块划分

### 7.1 项目结构

```
AgentFree/
├── src/
│   ├── AgentFree.API/                    # .NET 10 Web API
│   │   ├── Controllers/                  # API 控制器
│   │   │   ├── AgentsController.cs       # Agent CRUD
│   │   │   ├── ResponsesController.cs    # OpenAI Responses API
│   │   │   └── SessionsController.cs     # 会话管理
│   │   ├── Models/                       # 数据模型
│   │   ├── Data/                         # EF Core DbContext
│   │   ├── Core/                         # 核心引擎
│   │   │   ├── IAgent.cs                 # 智能体接口
│   │   │   ├── AgenticLoopEngine.cs      # 循环引擎
│   │   │   ├── IToolRegistry.cs          # 工具注册表
│   │   │   ├── IMemoryManager.cs         # 记忆管理
│   │   │   └── IPromptBuilder.cs         # 提示词构建
│   │   ├── Tools/                        # 内置工具
│   │   │   ├── FileTool.cs               # 文件操作
│   │   │   ├── HttpTool.cs               # HTTP 请求
│   │   │   └── ShellTool.cs              # 终端执行
│   │   ├── Adapters/                     # 协议适配器
│   │   │   ├── IProtocolAdapter.cs       # 协议适配器接口
│   │   │   ├── McpProtocolAdapter.cs     # MCP 适配器
│   │   │   ├── A2UIProtocolAdapter.cs    # A2UI 适配器
│   │   │   └── AGUIProtocolAdapter.cs    # AG-UI 适配器
│   │   ├── Services/                     # 业务服务
│   │   │   ├── ResponseService.cs        # 响应服务
│   │   │   └── AgentService.cs           # Agent 服务
│   │   ├── Program.cs                    # 启动配置
│   │   └── appsettings.json              # 配置
│   │
│   └── AgentFree.Web/                    # React 前端
│       ├── src/
│       │   ├── api/                      # API 服务
│       │   ├── components/               # 通用组件
│       │   ├── pages/                    # 页面
│       │   │   ├── Home.tsx              # 首页
│       │   │   ├── Agents.tsx            # Agent 管理
│       │   │   ├── Tools.tsx             # 工具管理
│       │   │   └── Sessions.tsx          # 会话管理
│       │   ├── types/                    # TypeScript 类型
│       │   └── App.tsx                   # 主布局
│       └── ...
│
├── tests/
│   └── AgentFree.Tests/                  # xUnit 测试
│       ├── Core/                         # 核心引擎测试
│       ├── Services/                     # 业务逻辑测试
│       └── Tools/                        # 工具测试
│
├── docs/
│   ├── requirements/                     # 需求文档
│   ├── design/                           # 设计文档
│   ├── code-review/                      # 代码审查
│   └── reports/                          # 报告
│
└── scripts/
    └── dev.sh                            # 开发脚本
```

---

## 8. 实施计划

### Phase 1: 核心引擎（P0）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T01 | 添加 Microsoft.Extensions.AI NuGet 包 | 无 |
| T02 | 实现 IAgent 接口和 AgenticLoopEngine | T01 |
| T03 | 实现 IToolRegistry 工具注册表 | T01 |
| T04 | 实现内置工具（File/HTTP/Shell） | T03 |
| T05 | 实现 IMemoryManager 记忆管理 | T02 |
| T06 | 实现 IPromptBuilder 提示词构建 | T02 |
| T07 | 编写核心引擎单元测试 | T02-T06 |

### Phase 2: OpenAI Responses API兼容层（P0）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T08 | 实现 IResponseService 接口 | T02 |
| T09 | 实现 ResponsesController（流式 + 标准） | T08 |
| T10 | 实现 OpenAI 格式请求/响应模型 | T08 |
| T11 | 实现 SSE 流式响应 | T09 |
| T12 | 集成测试（OpenAI SDK 测试） | T09 |

### Phase 3: 协议适配器层（P1）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T13 | 实现 IProtocolAdapter 统一接口 | 无 |
| T14 | 实现 ProtocolManager | T13 |
| T15 | 实现 MCP 协议适配器 | T13 |
| T16 | 实现 A2UI 协议适配器 | T13 |
| T17 | 实现 AG-UI 协议适配器 | T13 |

### Phase 4: 数据层与 API 层（P0）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T18 | 数据库切换：InMemory → PostgreSQL | 无 |
| T19 | 实现 EF Core DbContext | T18 |
| T20 | 实现 Agent/Session/Message/Tool/Memory 表迁移 | T19 |
| T21 | 实现 AgentService 业务逻辑 | T20 |
| T22 | 实现 SessionService 业务逻辑 | T20 |
| T23 | 实现 ToolService 业务逻辑 | T20 |

### Phase 5: 前端改造（P1）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T24 | 移除 ProtocolType 字段 | 无 |
| T25 | 添加 Sessions 页面 | T22 |
| T26 | 添加 Tools 管理页面 | T23 |
| T27 | 添加 Agent Execute 页面 | T21 |
| T28 | 更新 Agent 表单（移除协议选项） | T24 |

### Phase 6: 测试与部署（P1）

| 任务 | 描述 | 依赖 |
|------|------|------|
| T29 | 编写单元测试（核心引擎） | T02-T07 |
| T30 | 编写集成测试（API 层） | T09 |
| T31 | 性能测试 | T12 |
| T32 | 代码审查 | T30 |
| T33 | 部署到测试环境 | T32 |
| T34 | 用户验收测试 | T33 |

---

## 9. 关键设计决策

### 9.1 为什么不直接用 Microsoft Agent Framework？

| 维度 | 原因 |
|------|------|
| **通用性** | MS 框架是通用框架，我们需要的是特定场景的实现 |
| **可控性** | 自己实现可以更精细地控制行为和性能 |
| **集成** | 需要集成到现有 .NET DI 生态，自定义程度高 |
| **学习成本** | 直接使用 NuGet 包解决最基础的 LLM 抽象，上层自己实现 |

### 9.2 为什么 ProtocolType 字段要移除？

| 维度 | 原因 |
|------|------|
| **设计错误** | 协议适配器是内部实现，不是用户配置项 |
| **混淆** | 用户不应该在创建时选择协议类型 |
| **扩展性** | 内部机制应该透明，通过 IProtocolManager 统一管理 |

### 9.3 为什么用 PostgreSQL 而不是 InMemory？

| 维度 | 原因 |
|------|------|
| **持久化** | InMemory 只能用于测试，生产环境必须持久化 |
| **扩展性** | PostgreSQL 支持向量搜索（Memory Embedding） |
| **生态** | 已经是项目默认数据库 |
| **迁移** | EF Core 无缝支持 |

---

## 10. 风险与约束

### 10.1 风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| LLM API 限制 | 并发限制/费用 | 实现请求队列/缓存 |
| 工具执行安全 | 恶意工具调用 | 实现沙箱隔离 |
| 上下文溢出 | Token 限制 | 实现会话压缩 |
| PostgreSQL 向量 | 扩展需求 | 使用 pgvector 扩展 |

### 10.2 约束

| 约束 | 说明 |
|------|------|
| **.NET 10** | 必须使用最新 .NET 版本 |
| **PostgreSQL** | 必须使用 PostgreSQL 17+ |
| **EF Core** | ORM 必须使用 EF Core |
| **React** | 前端必须使用 React + TypeScript |

---

## 11. 验收标准

### 11.1 功能验收

- [ ] Agent 可以正常创建、编辑、删除
- [ ] Agent 可以执行任务并返回结果
- [ ] 工具调用 → 执行 → 反馈循环正常工作
- [ ] 会话历史正确保存和检索
- [ ] 长期记忆支持语义搜索

### 11.2 性能验收

- [ ] API 响应时间 < 200ms（简单查询）
- [ ] 页面加载时间 < 2s
- [ ] 支持并发 Agent 执行

### 11.3 质量验收

- [ ] 单元测试覆盖率 > 70%
- [ ] 无严重安全漏洞
- [ ] 日志记录完整

---

## 12. 总结

AgentFree 采用 **分层架构**：

```
底层：Microsoft.Extensions.AI（标准库）
  ↓ 抽象 LLM 调用
中层：Agentic Loop 引擎（自实现）
  ↓ 编排工具 + 记忆
上层：API + 前端（业务层）
```

**核心原则**：
1. 用现成库解决底层问题（LLM 统一接口）
2. 自己实现核心引擎（Agentic Loop / 工具编排）
3. 移除设计错误（ProtocolType 字段）
4. 遵循项目规范（PROJECT-GUIDANCE.md）

---

> **下一步**：向志山确认此设计方案，确认后进入编码阶段。
