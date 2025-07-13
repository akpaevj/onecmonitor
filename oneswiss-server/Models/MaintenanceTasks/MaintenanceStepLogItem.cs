using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceStepLogItem : DatabaseObject
{
    [DataType(DataType.Date)]
    public DateTime TimeStamp { get; set; }
    public bool IsError { get; set; }
    public bool IsFinish { get; set; }
    [MaxLength(200)]
    public string Message { get; set; } = string.Empty;
    public Guid InfoBaseId { get; set; }
    public Guid StepId { get; set; } 
    
    [ForeignKey(nameof(InfoBaseId))]
    public virtual InfoBase InfoBase { get; set; } = null!;
    
    [ForeignKey(nameof(StepId))]
    public virtual MaintenanceStep Step { get; set; } = null!;
}