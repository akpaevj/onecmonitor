using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models;

public class Cluster : DatabaseObject
{
    public string ClusterId { get; set; } = null!;
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; } = null!;
    
    public Guid CredentialsId { get; set; }
    public Credentials Credentials { get; set; } = null!;
    
    public List<InfoBase> InfoBases { get; set; } = [];
}