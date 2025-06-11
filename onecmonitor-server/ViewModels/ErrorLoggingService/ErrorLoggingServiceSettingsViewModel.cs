namespace OnecMonitor.Server.ViewModels.ErrorLoggingService;

public class ErrorLoggingServiceSettingsViewModel
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public int ReportsTtl { get; set; }
}