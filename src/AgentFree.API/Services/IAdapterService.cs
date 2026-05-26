using AgentFree.API.Core;

namespace AgentFree.API.Services;

/// <summary>
/// 智能体适配器接口 — 封装不同智能体框架的通信协议
/// 每个适配器负责将本地标准接口（AgentFree协议）转换为特定智能体框架的通信格式
/// </summary>
public interface IAdapterService
{
    /// <summary>
    /// 适配器类型（Goldfish / Hermes / Openclaw）
    /// </summary>
    string AdapterType { get; }

    /// <summary>
    /// 执行对话 — 标准模式
    /// </summary>
    Task<AgentResponse> ChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// 执行对话 — 流式模式（A2UI / SSE）
    /// </summary>
    IAsyncEnumerable<StreamChunk> StreamChatAsync(
        AgentInfo agentInfo,
        string sessionId,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// 获取会话历史
    /// </summary>
    Task<IList<ChatMessage>> GetHistoryAsync(string sessionId);

    /// <summary>
    /// 取消对话
    /// </summary>
    Task CancelAsync(string sessionId);
}

/// <summary>
/// 流式响应块
/// </summary>
public class StreamChunk
{
    public string Delta { get; set; } = string.Empty;
    public bool Done { get; set; }
    public object? Metadata { get; set; }
}
