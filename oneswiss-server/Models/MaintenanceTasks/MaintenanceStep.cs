using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MudBlazor;
using OneSwiss.Common.Models.MaintenanceTasks;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceStep : DatabaseObject
{
    public Guid MaintenanceTaskId { get; set; }
    public MaintenanceStepKind Kind { get; set; }
    public MaintenanceStepNodeKind NodeKind { get; set; }
    public Guid? PreviousStepId { get; set; }
    public Guid? LeftStepId { get; set; }
    public Guid? RightStepId { get; set; }
    public Guid? LockConnectionsStepId { get; set; }
    public Guid? LoadConfigurationStepId { get; set; }
    public Guid? DeleteExtensionStepId { get; set; }
    public Guid? LoadExtensionStepId { get; set; }
    public Guid? UpdateConfigurationStepId { get; set; }
    public Guid? StartExternalDataProcessorStepId { get; set; }
    public Guid? ExecuteOneScriptStepId { get; set; }
    public Guid? CopyInfoBaseStepId { get; set; }
    
    // Вспомогательные свойства для UI ++
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    // Вспомогательные свойства для UI --

    [ForeignKey(nameof(MaintenanceTaskId))]
    public MaintenanceTask MaintenanceTask { get; set; } = null!;
    
    [ForeignKey(nameof(CopyInfoBaseStepId))]
    public CopyInfoBaseStep? CopyInfoBaseStep { get; set; }
    
    [ForeignKey(nameof(ExecuteOneScriptStepId))]
    public ExecuteOneScriptStep? ExecuteOneScriptStep { get; set; }
    
    [ForeignKey(nameof(StartExternalDataProcessorStepId))]
    public StartExternalDataProcessorStep? StartExternalDataProcessorStep { get; set; }
    
    [ForeignKey(nameof(UpdateConfigurationStepId))]
    public UpdateConfigurationStep? UpdateConfigurationStep { get; set; }
    
    [ForeignKey(nameof(LoadExtensionStepId))]
    public LoadExtensionStep? LoadExtensionStep { get; set; }
    
    [ForeignKey(nameof(DeleteExtensionStepId))]
    public DeleteExtensionStep? DeleteExtensionStep { get; set; }
    
    [ForeignKey(nameof(LoadConfigurationStepId))]
    public LoadConfigurationStep? LoadConfigurationStep { get; set; }
    
    [ForeignKey(nameof(LockConnectionsStepId))]
    public LockConnectionsStep? LockConnectionsStep { get; set; }
    
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
}