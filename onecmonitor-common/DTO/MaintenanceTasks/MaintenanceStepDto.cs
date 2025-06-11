using MessagePack;
using OnecMonitor.Common.Models.MaintenanceTasks;
using OneScript.Contexts;

namespace OnecMonitor.Common.DTO.MaintenanceTasks;

[ContextClass("ШагОбслуживания")]
[MessagePackObject]
public class MaintenanceStepDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [ContextProperty("Тип", "Type")]
    [Key(1)]
    public MaintenanceStepKind Kind { get; set; }
    [ContextProperty("ТипУзла", "NodeType")]
    [Key(2)]
    public MaintenanceStepNodeKind NodeKind { get; set; }
    [ContextProperty("ИдентификаторПредыдущегоШага", "PreviousStepId")]
    [Key(3)]
    public Guid? PreviousStepId { get; set; }
    [ContextProperty("ИдентификаторЛевогоШага", "LeftStepId")]
    [Key(4)]
    public Guid? LeftStepId { get; set; }
    [ContextProperty("ИдентификаторПравогоШага", "RightStepId")]
    [Key(5)]
    public Guid? RightStepId { get; set; }
    [ContextProperty("КодДоступа", "AccessCode")]
    [Key(6)]
    public string AccessCode { get; set; } = string.Empty;
    [ContextProperty("Сообщение", "Message")]
    [Key(7)]
    public string Message { get; set; } = string.Empty;
    [ContextProperty("Файл", "File")]
    [Key(8)] 
    public V8FileDto? File { get; set; }
    [ContextProperty("ИмяРасширения", "ExtensionName")]
    [Key(9)]
    public string ExtensionName { get; set; } = string.Empty;
}