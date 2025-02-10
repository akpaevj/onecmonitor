using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class InfoBase : DatabaseObject
{
    public string InfoBaseId { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string InfoBaseName { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    
    public Guid CredentialsId { get; set; }
    public Credentials Credentials { get; set; } = null!;
    
    public Guid ClusterId { get; set; }
    public Cluster Cluster { get; set; } = null!;
}