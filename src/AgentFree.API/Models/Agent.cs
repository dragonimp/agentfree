namespace AgentFree.API.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        
        // 四个核心参数
        public string? AgentId { get; set; }        // 智能体ID（用户自定义唯一标识）
        public required string AgentType { get; set; } = "Goldfish"; // 智能体类型
        public required string BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        
        public string Status { get; set; } = "Inactive";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateAgentDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? AgentId { get; set; }
        public required string AgentType { get; set; }
        public required string BaseUrl { get; set; }
        public string? ApiKey { get; set; }
    }

    public class UpdateAgentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? AgentId { get; set; }
        public string? AgentType { get; set; }
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
    }
}
