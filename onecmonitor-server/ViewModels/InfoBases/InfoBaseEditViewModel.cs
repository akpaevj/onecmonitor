using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.InfoBases;

public class InfoBaseEditViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    
    public Guid AgentId { get; set; }

    [ValidateNever] 
    public SelectList Agents { get; set; }
}