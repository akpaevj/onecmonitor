using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Maintenance;

public class MaintenanceStepViewModel
{
    public Guid Id { get; set; }
    public MaintenanceStepKind Kind { get; set; }
    
    [ValidateNever] 
    public Guid? FileId { get; set; }
    [ValidateNever] 
    public SelectList Files { get; set; } = null!;
    
    [ValidateNever] 
    public string AccessCode { get; set; } = string.Empty;
    [ValidateNever] 
    public string Message { get; set; } = string.Empty;
}