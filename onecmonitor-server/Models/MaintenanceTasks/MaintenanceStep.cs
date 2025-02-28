using System.ComponentModel.DataAnnotations;
using OnecMonitor.Common.Models.MaintenanceTasks;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public class MaintenanceStep : DatabaseObject
{
    public MaintenanceStepKind Kind { get; set; }
    
    [MaxLength(20)] 
    public string AccessCode { get; set; } = string.Empty;
    [MaxLength(200)] 
    public string Message { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    
    public V8File? File { get; set; }
}