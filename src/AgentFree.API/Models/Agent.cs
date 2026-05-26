namespace AgentFree.API.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string AgentType { get; set; } = "Goldfish";
        public string? ServiceUrl { get; set; }
        public string? AgentId { get; set; }
        public string? Token { get; set; }
        public string Status { get; set; } = "Inactive";
        
        // 对话大模型类型专用字段
        public string? LLMProvider { get; set; }       // OpenAI, Ollama, Azure 等
        public string? LLMBaseUrl { get; set; }          // API 地址
        public string? LLMModelName { get; set; }         // 模型名称
        public string? LLMApiKey { get; set; }            // API 密钥
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateAgentDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string AgentType { get; set; } = "Goldfish";
        public string? ServiceUrl { get; set; }
        public string? AgentId { get; set; }
        public string? Token { get; set; }
        
        // 对话大模型类型专用字段
        public string? LLMProvider { get; set; }
        public string? LLMBaseUrl { get; set; }
        public string? LLMModelName { get; set; }
        public string? LLMApiKey { get; set; }
    }

    public class UpdateAgentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string? AgentType { get; set; }
        public string? ServiceUrl { get; set; }
        public string? AgentId { get; set; }
        public string? Token { get; set; }
        public string? Status { get; set; }
        
        // 对话大模型类型专用字段
        public string? LLMProvider { get; set; }
        public string? LLMBaseUrl { get; set; }
        public string? LLMModelName { get; set; }
        public string? LLMApiKey { get; set; }
    }
}
