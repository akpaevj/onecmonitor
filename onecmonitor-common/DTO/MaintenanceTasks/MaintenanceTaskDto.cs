using MessagePack;
using OneScript.Contexts;

namespace OnecMonitor.Common.DTO.MaintenanceTasks;

[ContextClass("ЗадачаОбслуживания", "MaintenanceTasks")]
[MessagePackObject]
public class MaintenanceTaskDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [ContextProperty("Шаги", "Steps")]
    [Key(1)]
    public List<MaintenanceStepDto> Steps { get; set; } = [];
    [ContextProperty("ИнформационныеБазы", "InfoBases")]
    [Key(2)] 
    public List<InfoBaseDto> InfoBases { get; set; } = [];
}