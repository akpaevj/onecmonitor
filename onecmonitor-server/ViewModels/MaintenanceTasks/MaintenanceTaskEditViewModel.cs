using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTaskEditViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Описание")]
    public string Description { get; set; } = string.Empty;
    public bool IsTemplate { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    
    [DisplayName("Информационные базы")]
    public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] 
    public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];
    [ValidateNever] 
    public string Steps { get; set; } = string.Empty;
}