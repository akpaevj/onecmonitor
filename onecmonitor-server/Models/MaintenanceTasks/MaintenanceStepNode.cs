using System.ComponentModel.DataAnnotations.Schema;
using OnecMonitor.Common.Models.MaintenanceTasks;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceStepNode : DatabaseObject
{
    public MaintenanceStepNodeKind Kind { get; set; }
    
    public Guid? LeftNodeId { get; set; }
    public Guid? RightNodeId { get; set; }
    public Guid StepId { get; set; }
    
    [ForeignKey(nameof(LeftNodeId))]
    public MaintenanceStepNode LeftNode { get; set; } = null!;
    [ForeignKey(nameof(RightNodeId))]
    public MaintenanceStepNode RightNode { get; set; } = null!;
    [ForeignKey(nameof(StepId))]
    public MaintenanceStep Step { get; set; } = null!;

    public virtual List<MaintenanceStepNodeLogItem> Logs { get; set; } = [];
}