namespace AgentFree.API.Models;

public class Session
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int AgentId { get; set; }
    public Agent? Agent { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateSessionDto
{
    public int AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateSessionDto
{
    public string? Name { get; set; }
}
