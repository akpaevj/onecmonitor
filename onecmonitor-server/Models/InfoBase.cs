using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.Models;

public class InfoBase : DatabaseObject
{
    public string InfoBaseInternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InfoBaseName { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    public Guid? CredentialsId { get; set; }
    public Guid ClusterId { get; set; }
    
    public virtual Credentials? Credentials { get; set; }
    public virtual Cluster Cluster { get; set; } = null!;
    
    public virtual List<MaintenanceTask> MaintenanceTasks { get; set; } = [];
}