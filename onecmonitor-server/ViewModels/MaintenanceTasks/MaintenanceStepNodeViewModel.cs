using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceStepNodeViewModel
{
    public Guid Id { get; set; }
    public MaintenanceStepNodeKind Kind { get; set; }
    
    [ValidateNever] 
    public Guid? LeftNodeId { get; set; }
    [ValidateNever]
    public Guid? RightNodeId { get; set; }
    
    public Guid StepId { get; set; }
    
    public MaintenanceStepNodeViewModel? LeftNode { get; set; } = null!;
    public MaintenanceStepNodeViewModel? RightNode { get; set; } = null!;
    
    public MaintenanceStepViewModel Step { get; set; } = null!;
}