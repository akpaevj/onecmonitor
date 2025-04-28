using MessagePack;
using OnecMonitor.Common.Models.MaintenanceTasks;

namespace OnecMonitor.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceStepDto
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [Key(1)]
    public MaintenanceStepKind Kind { get; set; }
    [Key(2)]
    public MaintenanceStepNodeKind NodeKind { get; set; }
    [Key(3)]
    public Guid? PreviousStepId { get; set; }
    [Key(4)]
    public Guid? LeftStepId { get; set; }
    [Key(5)]
    public Guid? RightStepId { get; set; }
    [Key(6)]
    public string AccessCode { get; set; } = string.Empty;
    [Key(7)]
    public string Message { get; set; } = string.Empty;
    [Key(8)] 
    public V8FileDto? File { get; set; }
    [Key(9)]
    public string ExtensionName { get; set; } = string.Empty;
}