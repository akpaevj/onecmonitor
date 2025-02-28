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
    public string AccessCode { get; set; } = string.Empty;
    [Key(3)]
    public string Message { get; set; } = string.Empty;
    [Key(4)]
    public V8FileDto? File { get; set; }
}