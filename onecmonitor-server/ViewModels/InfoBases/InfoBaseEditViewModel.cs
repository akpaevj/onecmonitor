using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.InfoBases;

public class InfoBaseEditViewModel
{
    public Guid Id { get; set; }
    public string InfoBaseInternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InfoBaseName { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    
    public Guid CredentialsId { get; set; }
    [ValidateNever] public SelectList Credentials { get; set; } = null!;
    public Guid ClusterId { get; set; }
    [ValidateNever] public SelectList Clusters { get; set; } = null!;
}