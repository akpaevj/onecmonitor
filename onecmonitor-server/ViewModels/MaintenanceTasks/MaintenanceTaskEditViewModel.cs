using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTaskEditViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Описание")]
    public string Description { get; set; } = string.Empty;
    
    [DisplayName("Информационные базы")]
    public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] 
    public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];

    public string SerializedStepNode { get; set; } = string.Empty;
}