using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Clusters;

public class ClusterEditViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1540;

    public Guid AgentId { get; set; }
    [ValidateNever] public SelectList Agents { get; set; } = null!;
    
    [ValidateNever] public List<SelectableItem> InfoBases { get; set; } = [];
    [ValidateNever] public List<SelectableItem> AvailableInfoBases { get; set; } = [];
}