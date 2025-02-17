using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.Clusters;

public class ClusterEditViewModel
{
    public Guid Id { get; set; }
    public string ClusterInternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1540;

    public Guid? CredentialsId { get; set; }
    [ValidateNever] public SelectList Credentials { get; set; } = null!;
    
    public Guid AgentId { get; set; }
    [DisplayName("Агенты")]
    [ValidateNever] 
    public SelectList Agents { get; set; } = null!;
    
    
    [ValidateNever] 
    [DisplayName("Информационные базы")]
    public List<SelectableItemViewModel> InfoBases { get; set; } = [];
    [ValidateNever] public List<SelectableItemViewModel> AvailableInfoBases { get; set; } = [];
}