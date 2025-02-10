using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OnecMonitor.Server.Models;
using OneSTools.Common.Platform;

namespace OnecMonitor.Server.ViewModels.Agents;

[ValidateNever]
public class AgentEditViewModel
{
    public Guid Id { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public List<V8Platform> InstalledPlatforms { get; set; } = [];
    public List<V8Service> Services { get; set; } = [];
    
    public List<SelectableItemViewModel> Clusters { get; set; } = [];
    public List<SelectableItemViewModel> AvailableClusters { get; set; } = [];
}