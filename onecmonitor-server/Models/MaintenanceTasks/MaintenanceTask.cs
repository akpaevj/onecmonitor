using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceTask : DatabaseObject
{
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    public Guid RootNodeId { get; set; }
    
    [ForeignKey(nameof(RootNodeId))]
    public MaintenanceStepNode RootNode { get; set; } = null!;
    
    public virtual List<InfoBase> InfoBases { get; set; } = [];
}