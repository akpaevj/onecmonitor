using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using OneSwiss.Common.Models.MaintenanceTasks;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class MaintenanceStep : DatabaseObject
{
    public Guid StepId { get; set; }
    public Guid MaintenanceTaskId { get; set; }
    public MaintenanceStepKind Kind { get; set; }
    public MaintenanceStepNodeKind NodeKind { get; set; }
    public Guid? PreviousStepId { get; set; }
    public Guid? LeftStepId { get; set; }
    public Guid? RightStepId { get; set; }
    
    // Вспомогательные свойства для UI ++
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    // Вспомогательные свойства для UI --

    [ForeignKey(nameof(MaintenanceTaskId))]
    public MaintenanceTask MaintenanceTask { get; set; } = null!;
    public CopyInfoBaseStep? CopyInfoBaseStep { get; set; } = new();
    public ExecuteOneScriptStep? ExecuteOneScriptStep { get; set; } = new();
    public StartExternalDataProcessorStep? StartExternalDataProcessorStep { get; set; } = new();
    public UpdateConfigurationStep? UpdateConfigurationStep { get; set; } = new();
    public LoadExtensionStep? LoadExtensionStep { get; set; } = new();
    public DeleteExtensionStep? DeleteExtensionStep { get; set; } = new();
    public LoadConfigurationStep? LoadConfigurationStep { get; set; } = new();
    public LockConnectionsStep? LockConnectionsStep { get; set; } = new();
    
    public virtual List<MaintenanceTaskLogItem> Logs { get; set; } = [];
}