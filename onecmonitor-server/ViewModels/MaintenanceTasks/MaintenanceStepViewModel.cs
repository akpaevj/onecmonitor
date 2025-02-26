using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Server.Extensions;
using OnecMonitor.Server.Helpers;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceStepViewModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("kind")]
    public MaintenanceStepKind Kind { get; set; }
    [ValidateNever] 
    public SelectList Kinds { get; set; } = null!;
    
    [ValidateNever] 
    [JsonPropertyName("accessCode")]
    public string AccessCode { get; set; } = string.Empty;
    [ValidateNever] 
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [ValidateNever] 
    [JsonPropertyName("fileId")]
    public Guid? FileId { get; set; }
    [ValidateNever] 
    public SelectList Files { get; set; } = null!;
    
    [JsonPropertyName("title")]
    public string Title => Kind.GetDisplay();
}