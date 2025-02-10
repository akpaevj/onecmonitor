using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class InfoBase : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string InfoBaseName { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    
    public Guid ClusterId { get; set; }
    public Cluster Cluster { get; set; } = null!;
}