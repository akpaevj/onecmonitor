using OneSwiss.Server.Models;

namespace OneSwiss.Server.Components.Pages.ErrorLoggingService;

public class ErrorReportViewModel : IHasId
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Configuration { get; set; } = string.Empty;
    public string ConfigurationVersion { get; set; } = string.Empty;
    public string PlatformVersion { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}