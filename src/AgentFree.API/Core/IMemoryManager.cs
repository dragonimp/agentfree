namespace AgentFree.API.Core;

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
/// 记忆管理器内存实现（基于内存存储）
/// </summary>
public class InMemoryMemoryManager : IMemoryManager
{
    private readonly Dictionary<string, List<ChatMessage>> _sessions = new();
    private readonly List<MemoryEntry> _longTermMemories = new();
    private readonly object _lock = new();

    public async Task AddMessageAsync(string sessionId, ChatMessage message)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var messages))
            {
                messages = new List<ChatMessage>();
                _sessions[sessionId] = messages;
            }
            messages.Add(message);
        }
        await Task.CompletedTask;
    }

    public async Task<IList<ChatMessage>> GetHistoryAsync(string sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var messages)
                ? messages.ToList().AsReadOnly()
                : new List<ChatMessage>();
        }
    }

    public async Task AddMemoryAsync(MemoryEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Id))
            entry.Id = Guid.NewGuid().ToString("N");

        lock (_lock)
        {
            _longTermMemories.Add(entry);
        }
        await Task.CompletedTask;
    }

    public async Task<IList<MemoryEntry>> SearchAsync(string query, int limit = 5)
    {
        lock (_lock)
        {
            var results = _longTermMemories
                .Where(m => string.IsNullOrEmpty(query) ||
                            m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (m.Category != null && m.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToList();

            return results;
        }
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        lock (_lock)
        {
            _sessions.Remove(sessionId);
        }
        await Task.CompletedTask;
    }

    public async Task<IList<ChatMessage>> CompressAsync(string sessionId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var messages) || messages.Count <= 3)
            {
                var messagesReadOnly = messages?.ToList();
                return messagesReadOnly ?? new List<ChatMessage>();
            }

            // 保留最近的消息 + 压缩早期消息
            var recent = messages.Skip(Math.Max(0, messages.Count - 3)).ToList();
            var earlyMessages = messages.Take(messages.Count - 3).ToList();

            // 将早期消息汇总为一个摘要
            if (earlyMessages.Any())
            {
                var summary = new ChatMessage
                {
                    Role = "system",
                    Content = $"[早期对话摘要 - {earlyMessages.Count} 条消息已压缩]",
                    CreatedAt = earlyMessages.First().CreatedAt
                };
                recent.Insert(0, summary);
            }

            _sessions[sessionId] = recent;
            return recent.AsReadOnly();
        }
    }
}
