using MessagePack;
using OneScript.Contexts;
using OneSwiss.Common.Models.MaintenanceTasks;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

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
    [Key(6)] 
    public CopyInfoBaseStepDto? CopyInfoBaseStep { get; set; }
    [Key(7)] 
    public DeleteExtensionStepDto? DeleteExtensionStep { get; set; }
    [Key(8)] 
    public ExecuteOneScriptStepDto? ExecuteOneScriptStep { get; set; }
    [Key(9)] 
    public LoadConfigurationStepDto? LoadConfigurationStep { get; set; }
    [Key(10)] 
    public LoadExtensionStepDto? LoadExtensionStep { get; set; }
    [Key(11)] 
    public LockConnectionsStepDto? LockConnectionsStep { get; set; }
    [Key(12)] 
    public StartExternalDataProcessorStepDto? StartExternalDataProcessorStep { get; set; }
    [Key(13)] 
    public UpdateConfigurationStepDto? UpdateConfigurationStep { get; set; }
}