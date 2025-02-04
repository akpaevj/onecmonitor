using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class InfoBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; }
}