using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceTask : DatabaseObject
{
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    public bool IsFaulted { get; set; } = false;
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    public Guid RootNodeId { get; set; }
    
    [ForeignKey(nameof(RootNodeId))]
    public MaintenanceStepNode RootNode { get; set; } = null!;
    
    public virtual List<InfoBase> InfoBases { get; set; } = [];
}