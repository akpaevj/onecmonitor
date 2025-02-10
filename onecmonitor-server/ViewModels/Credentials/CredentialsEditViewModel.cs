using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Credentials;

public class CredentialsEditViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool DefaultForClusters { get; set; } = false;
    public bool DefaultV8Admin { get; set; } = false;
    
    [ValidateNever] public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];
    
    [ValidateNever] public List<SelectableItemViewModel> Clusters { get; set; } = [];
    [ValidateNever] public List<SelectableItemViewModel> AvailableClusters { get; set; } = [];
}