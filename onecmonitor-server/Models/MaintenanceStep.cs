using System.Configuration;

namespace OnecMonitor.Server.Models;

public abstract class MaintenanceStep
{
    public MaintenanceStepKind Kind { get; set; }
    public Configuration? Configuration { get; set; }
    public string AccessCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}