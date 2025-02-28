using MessagePack;
using OnecMonitor.Common.Models.MaintenanceTasks;

namespace OnecMonitor.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceStepNodeDto
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [Key(1)]
    public MaintenanceStepNodeKind Kind { get; set; }
    [Key(2)]
    public MaintenanceStepNodeDto? LeftNode { get; set; }
    [Key(3)]
    public MaintenanceStepNodeDto? RightNode { get; set; }
    [Key(4)] 
    public MaintenanceStepDto Step { get; set; } = null!;
}