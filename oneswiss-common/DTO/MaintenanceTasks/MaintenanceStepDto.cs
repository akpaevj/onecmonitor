using MessagePack;
using OneScript.Contexts;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагОбслуживания", "MaintenanceStep")]
[MessagePackObject]
public class MaintenanceStepDto
{
    [ContextProperty("Идентификатор", "Id", Converter = typeof(GuidContextConverter))]
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;
    [ContextProperty("ИдентификаторШага", "StepId", Converter = typeof(GuidContextConverter))]
    [Key(1)]
    public Guid StepId { get; set; } = Guid.Empty;
    [Key(2)]
    [ContextProperty("ТипШага", "StepKind")]
    public MaintenanceStepKind Kind { get; set; }
    [Key(3)]
    [ContextProperty("ТипНодыШага", "StepNodeKind")]
    public MaintenanceStepNodeKind NodeKind { get; set; }
    [Key(4)]
    [ContextProperty("ИдентификаторПредыдущегоШага", "PreviousStepId", Converter = typeof(GuidContextConverter))]
    public Guid? PreviousStepId { get; set; }
    [Key(5)]
    [ContextProperty("ИдентификаторЛевогоШага", "LeftStepId", Converter = typeof(GuidContextConverter))]
    public Guid? LeftStepId { get; set; }
    [Key(6)]
    [ContextProperty("ИдентификаторПравогоШага", "RightStepId", Converter = typeof(GuidContextConverter))]
    public Guid? RightStepId { get; set; }
    [Key(7)]
    [ContextProperty("ШагКопированияИнформационнойБазы", "CopyInfoBaseStep", Converter = typeof(NullableConverter<CopyInfoBaseStepDto>))]
    public CopyInfoBaseStepDto? CopyInfoBaseStep { get; set; }
    [Key(8)]
    [ContextProperty("ШагУдаленияРасширения", "DeleteExtensionStep", Converter = typeof(NullableConverter<DeleteExtensionStepDto>))]
    public DeleteExtensionStepDto? DeleteExtensionStep { get; set; }
    [Key(9)] 
    [ContextProperty("ШагИсполненияОскрипта", "ExecuteOneScriptStep", Converter = typeof(NullableConverter<ExecuteOneScriptStepDto>))]
    public ExecuteOneScriptStepDto? ExecuteOneScriptStep { get; set; }
    [Key(10)] 
    [ContextProperty("ШагЗагрузкиКонфигурации", "LoadConfigurationStep", Converter = typeof(NullableConverter<LoadConfigurationStepDto>))]
    public LoadConfigurationStepDto? LoadConfigurationStep { get; set; }
    [Key(11)] 
    [ContextProperty("ШагЗагрузкиРасширения", "LoadExtensionStep", Converter = typeof(NullableConverter<LoadExtensionStepDto>))]
    public LoadExtensionStepDto? LoadExtensionStep { get; set; }
    [Key(12)] 
    [ContextProperty("ШагБлокировкиСоединений", "LockConnectionsStep", Converter = typeof(NullableConverter<LockConnectionsStepDto>))]
    public LockConnectionsStepDto? LockConnectionsStep { get; set; }
    [Key(13)]
    [ContextProperty("ШагЗапускаВнешнейОбработки", "StartExternalDataProcessorStep", Converter = typeof(NullableConverter<StartExternalDataProcessorStepDto>))]
    public StartExternalDataProcessorStepDto? StartExternalDataProcessorStep { get; set; }
    [Key(14)] 
    [ContextProperty("ШагОбновленияКонфигурации", "UpdateConfigurationStep", Converter = typeof(NullableConverter<UpdateConfigurationStepDto>))]
    public UpdateConfigurationStepDto? UpdateConfigurationStep { get; set; }
}