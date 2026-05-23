namespace AgentFree.API.Models.Response;

// ==================== Request Models ====================

/// <summary>
/// 创建响应请求（兼容OpenAI Responses API）
/// </summary>
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

/// <summary>
/// 输入项
/// </summary>
public class ResponseInputItem
{
    public string Type { get; set; } = "message"; // message/file
    public string Role { get; set; } = "user"; // user/assistant
    public string Content { get; set; } = string.Empty;
    public string? FileId { get; set; } // 文件类型时
}

/// <summary>
/// 配置
/// </summary>
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

/// <summary>
/// 工具定义（与OpenAI格式兼容）
/// </summary>
public class ResponseTool
{
    public string Type { get; set; } = "function";
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersSchema { get; set; } = "{}"; // JSON Schema
    public bool? Strict { get; set; }
}

/// <summary>
/// 响应格式
/// </summary>
public class ResponseFormat
{
    public string Type { get; set; } = "text"; // text/json_object
    public string? JsonSchema { get; set; }
}

// ==================== Response Models ====================

/// <summary>
/// 标准响应
/// </summary>
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

/// <summary>
/// 输出项
/// </summary>
public class ResponseOutputItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "message"; // message/tool_call
    public string Status { get; set; } = "completed";
    public string? Role { get; set; } // assistant
    public string? Content { get; set; }
    public ResponseToolCall? ToolCall { get; set; }
}

/// <summary>
/// 工具调用
/// </summary>
public class ResponseToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "function_call";
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty; // JSON字符串
    public string? Status { get; set; }
}

/// <summary>
/// 使用情况
/// </summary>
public class ResponseUsage
{
    public int TotalTokens { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

// ==================== Streaming SSE Events ====================

/// <summary>
/// SSE 事件
/// </summary>
public class SseEvent
{
    public string Event { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Id { get; set; }

    public string Format()
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(Id))
            sb.AppendLine($"id: {Id}");
        sb.AppendLine($"event: {Event}");
        sb.AppendLine($"data: {System.Text.Json.JsonSerializer.Serialize(Data)}");
        sb.AppendLine();
        return sb.ToString();
    }
}
