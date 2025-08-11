using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ЗадачаОбслуживания")]
[MessagePackObject]
public class MaintenanceTaskDto
{
    [Key(0)]
    [ContextProperty("Идентификатор", "Id", Converter = typeof(GuidContextConverter))]
    public Guid Id { get; set; } = Guid.Empty;
    [Key(1)]
    [ContextProperty("ЭтоЗадачаОбщегоНазначения", "IsCommonDestination")]
    public bool CommonDestination { get; set; }
    [Key(2)]
    [ContextProperty("Шаги", "Steps", Converter = typeof(ListContextConverter<MaintenanceStepDto>))]
    public List<MaintenanceStepDto> Steps { get; set; } = [];
    [Key(3)] 
    [ContextProperty("ИнформационныеБазы", "InfoBases", Converter = typeof(ListContextConverter<InfoBaseDto>))]
    public List<InfoBaseDto> InfoBases { get; set; } = [];
}