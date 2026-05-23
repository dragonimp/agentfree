using AgentFree.API.Core;
using AgentFree.API.Models.Response;

namespace AgentFree.API.Services;

/// <summary>
/// 响应服务接口 — 核心业务逻辑
/// </summary>
public interface IResponseService
{
    /// <summary>
    /// 执行响应（标准模式）
    /// </summary>
    Task<ResponseOutput> ExecuteAsync(CreateResponseRequest request, CancellationToken ct = default);

    /// <summary>
    /// 获取响应详情
    /// </summary>
    Task<ResponseOutput?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// 取消响应
    /// </summary>
    Task CancelAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// 流式执行响应（SSE）
    /// </summary>
    IAsyncEnumerable<SseEvent> ExecuteStreamAsync(CreateResponseRequest request, CancellationToken ct = default);
}
