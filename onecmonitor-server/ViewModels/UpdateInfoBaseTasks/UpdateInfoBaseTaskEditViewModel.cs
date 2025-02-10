using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

public class UpdateInfoBaseTaskEditViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;

    public bool NeedUpdateConfiguration { get; } = false;
    
    public Guid ConfigurationId { get; set; }
    [ValidateNever] public SelectList Configurations { get; set; } = null!;
    
    [ValidateNever] public List<SelectableItem> Extensions { get; set; } = [];
    [ValidateNever] public List<SelectableItem> AvailableExtensions { get; set; } = [];
    
    public List<SelectableItem> InfoBases { get; set; } = [];
    [ValidateNever] public List<SelectableItem> AvailableInfoBases { get; set; } = [];
}