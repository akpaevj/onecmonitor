using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceStepNodeLogItem : DatabaseObject
{
    [DataType(DataType.Date)]
    public DateTime TimeStamp { get; set; }
    public bool IsError { get; set; }
    public bool IsFinish { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid InfoBaseId { get; set; }
    public Guid StepNodeId { get; set; } 
    
    [ForeignKey(nameof(InfoBaseId))]
    public virtual InfoBase InfoBase { get; set; } = null!;
    [ForeignKey(nameof(StepNodeId))]
    public virtual MaintenanceStepNode StepNode { get; set; } = null!;
}