using MessagePack;

namespace OnecMonitor.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceTaskDto
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [Key(1)]
    public List<MaintenanceStepDto> Steps { get; set; } = [];
    [Key(2)] 
    public List<InfoBaseDto> InfoBases { get; set; } = [];
}