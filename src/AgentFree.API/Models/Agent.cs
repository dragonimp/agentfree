namespace AgentFree.API.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string AgentType { get; set; } = "Goldfish";
        public string Status { get; set; } = "Inactive";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateAgentDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string AgentType { get; set; } = "Goldfish";
    }

    public class UpdateAgentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string? AgentType { get; set; }
        public string? Status { get; set; }
    }
}
