using MessagePack;
using OneScript.Contexts;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceStepDto
{
    [Key(0)]
    public Guid Id { get; set; } = Guid.Empty;

    [Key(1)]
    public Guid StepId { get; set; } = Guid.Empty;

    [Key(2)]
    public MaintenanceStepKind Kind { get; set; }

    [Key(3)]
    public MaintenanceStepNodeKind NodeKind { get; set; }

    [Key(4)]
    public Guid? PreviousStepId { get; set; }

    [Key(5)]
    public Guid? LeftStepId { get; set; }

    [Key(6)]
    public Guid? RightStepId { get; set; }

    [Key(7)]
    public CopyInfoBaseStepDto? CopyInfoBaseStep { get; set; }

    [Key(8)]
    public DeleteExtensionStepDto? DeleteExtensionStep { get; set; }

    [Key(9)]
    public ExecuteOneScriptStepDto? ExecuteOneScriptStep { get; set; }

    [Key(10)]
    public LoadConfigurationStepDto? LoadConfigurationStep { get; set; }

    [Key(11)]
    public LoadExtensionStepDto? LoadExtensionStep { get; set; }

    [Key(12)]
    public LockConnectionsStepDto? LockConnectionsStep { get; set; }

    [Key(13)]
    public StartExternalDataProcessorStepDto? StartExternalDataProcessorStep { get; set; }

    [Key(14)]
    public UpdateConfigurationStepDto? UpdateConfigurationStep { get; set; }
}