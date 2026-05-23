namespace AgentFree.API.Models;

public class Message
{
    public long Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // system/user/assistant/tool
    public string Content { get; set; } = string.Empty;
    public string? ToolCallId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? ToolCallId { get; set; }
}
