using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

public class UpdateInfoBaseTaskEditViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    
    [ValidateNever] public List<SelectableItemViewModel> Configurations { get; set; } = [];
    [ValidateNever] public List<SelectableItemViewModel> AvailableConfigurations { get; set; } = [];
    
    public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];
}