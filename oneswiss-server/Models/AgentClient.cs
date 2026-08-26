namespace OneSwiss.Server.Models;

public class AgentClient : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
