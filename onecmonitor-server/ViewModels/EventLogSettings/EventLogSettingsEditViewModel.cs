using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnecMonitor.Server.ViewModels.EventLogSettings;

public class EventLogSettingsEditViewModel
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = false;
    public Guid? DbmsId { get; set; }
    [ValidateNever] 
    public SelectList AvailableDbms { get; set; } = null!;
    public string DatabaseName { get; set; } = "oneswiss-server";
    public string Table { get; set; } = "eventlog";
    public Guid? CredentialsId { get; set; }
    [ValidateNever] 
    public SelectList AvailableCredentials { get; set; } = null!;
    [ValidateNever]
    public string InfoBaseNameRegex  { get; set; } = string.Empty;
}