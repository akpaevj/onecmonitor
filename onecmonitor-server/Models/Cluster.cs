using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models;

public class Cluster : DatabaseObject
{
    public string ClusterInternalId { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public Guid AgentId { get; set; }
    public Guid? CredentialsId { get; set; }
    
    public virtual Agent Agent { get; set; } = null!;
    public virtual Credentials? Credentials { get; set; } = null!;
    public virtual List<InfoBase> InfoBases { get; set; } = [];
}