using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneSwiss.Common.Models.MaintenanceTasks;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceStep : DatabaseObject
{
    public Guid MaintenanceTaskId { get; set; }
    
    public MaintenanceStepKind Kind { get; set; }
    public MaintenanceStepNodeKind NodeKind { get; set; }
    
    public Guid? PreviousStepId { get; set; }
    public Guid? LeftStepId { get; set; }
    public Guid? RightStepId { get; set; }
    
    [MaxLength(20)] 
    public string AccessCode { get; set; } = string.Empty;
    [MaxLength(200)] 
    public string Message { get; set; } = string.Empty;
    [MaxLength(1000)] 
    public string CommandLineArguments { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    
    [ForeignKey(nameof(FileId))]
    public V8File? File { get; set; }
    
    [MaxLength(200)] 
    public string ExtensionName { get; set; } = string.Empty;

    [ForeignKey(nameof(MaintenanceTaskId))]
    public MaintenanceTask MaintenanceTask { get; set; } = null!;
    public virtual List<MaintenanceStepLogItem> Logs { get; set; } = [];
}