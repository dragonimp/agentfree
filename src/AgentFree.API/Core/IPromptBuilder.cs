namespace AgentFree.API.Core;

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

/// <summary>
/// 提示词构建器实现
/// </summary>
public class PromptBuilder : IPromptBuilder
{
    public string BuildSystemPrompt(AgentInfo agent)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(agent.SystemPrompt ?? "You are a helpful AI assistant.");

        // 添加工具列表信息
        if (agent.ToolIds.Any())
        {
            sb.AppendLine("\n## Available Tools");
            sb.AppendLine("You have access to the following tools. If the user's request requires using a tool, call it appropriately.");
        }

        return sb.ToString().TrimEnd();
    }

    public IList<ChatMessage> BuildMessages(
        AgentInfo agent,
        string userPrompt,
        IList<ChatMessage> history,
        IList<ITool> tools)
    {
        var messages = new List<ChatMessage>();

        // 添加系统消息
        var systemPrompt = BuildSystemPrompt(agent);
        messages.Add(new ChatMessage
        {
            Role = "system",
            Content = systemPrompt
        });

        // 添加工具定义（如果有工具）
        if (tools.Any())
        {
            var toolDefinitions = string.Join("\n", tools.Select(t =>
                $"- Tool: {t.Name}\n  Description: {t.Description}\n  Schema: {t.ParametersSchema}"));

            messages.Add(new ChatMessage
            {
                Role = "system",
                Content = $"## Available Tools\n{toolDefinitions}\n\nWhen you need to use a tool, respond with a tool call in the specified format."
            });
        }

        // 添加历史消息
        foreach (var msg in history)
        {
            messages.Add(new ChatMessage
            {
                Role = msg.Role,
                Content = msg.Content,
                ToolCallId = msg.ToolCallId
            });
        }

        // 添加用户消息
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = userPrompt
        });

        return messages;
    }

    public void AddToolResult(IList<ChatMessage> messages, ToolCallRecord record)
    {
        messages.Add(new ChatMessage
        {
            Role = "tool",
            Content = record.Success ? record.Result : $"Error: {record.Result}",
            ToolCallId = record.ToolId
        });
    }
}
