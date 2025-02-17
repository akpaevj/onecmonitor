using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Maintenance;

public class MaintenanceEditViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] 
    public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];

    public List<MaintenanceStepViewModel> MaintenanceSteps { get; set; } = [];
    
    [ValidateNever] 
    public SelectList MaintenanceStepKinds { get; set; } = null!;
    
    [ValidateNever] 
    public SelectList ExternalDataProcessors { get; set; } = null!;
    
    [ValidateNever] 
    public SelectList Extensions { get; set; } = null!;
    
    [ValidateNever] 
    public SelectList ConfigUpdates { get; set; } = null!;
    
    [ValidateNever] 
    public SelectList Configs { get; set; } = null!;
}