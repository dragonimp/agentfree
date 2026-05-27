using AgentFree.API.Core;
using AgentFree.API.Data;
using AgentFree.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentFree.API.Services;

/// <summary>
/// 适配器路由器 — 根据 Agent 类型路由到对应的 IAdapterService 实现
/// Goldfish 类型 → GoldfishAdapter（OpenAI 兼容接口）
/// Hermes 类型 → HermesAdapter（跨框架通信）
/// 对话大模型 → DirectLLMAdapter（直接配置原始 LLM）
/// </summary>
public interface IAdapterRouter
{
    /// <summary>
    /// 选择适配器 — 根据 agentType 字符串选择对应适配器
    /// </summary>
    IAdapterService GetAdapter(string agentType);

    /// <summary>
    /// 列出所有可用适配器
    /// </summary>
    IReadOnlyList<AdapterInfo> GetAvailableAdapters();
}

public record AdapterInfo(string Type, string DisplayName, string Description);

/// <summary>
/// 适配器路由器实现
/// </summary>
public class AdapterRouter : IAdapterRouter
{
    private readonly IEnumerable<IAdapterService> _adapters;
    private readonly Dictionary<string, IAdapterService> _typeMap;
    private readonly IReadOnlyList<AdapterInfo> _adaptersList;

    public AdapterRouter(IEnumerable<IAdapterService> adapters)
    {
        _adapters = adapters;
        _typeMap = new Dictionary<string, IAdapterService>(StringComparer.OrdinalIgnoreCase);
        
        var adapterList = new List<AdapterInfo>();
        
        foreach (var adapter in adapters)
        {
            _typeMap[adapter.AdapterType] = adapter;
            adapterList.Add(CreateInfo(adapter));
        }
        
        _adaptersList = adapterList.AsReadOnly();
    }

    public IAdapterService GetAdapter(string agentType)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            agentType = "Goldfish"; // 默认

        if (_typeMap.TryGetValue(agentType, out var adapter))
            return adapter;

        throw new ArgumentException($"Unsupported agent type: {agentType}. Available: {string.Join(", ", _typeMap.Keys)}");
    }

    public IReadOnlyList<AdapterInfo> GetAvailableAdapters() => _adaptersList;

    private static AdapterInfo CreateInfo(IAdapterService adapter)
    {
        var desc = adapter.AdapterType switch
        {
            "Goldfish" => "OpenAI 兼容接口（直接 LLM 通信）",
            "Hermes" => "Hermes 框架（跨框架通信）",
            "对话大模型" => "直接配置原始大模型",
            _ => "未知适配器"
        };
        return new AdapterInfo(adapter.AdapterType, adapter.AdapterType, desc);
    }
}
