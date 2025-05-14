namespace OnecMonitor.Server.Dto.ErrorLoggingService;

public class ReportError
{
    public string Text { get; set; } = string.Empty;
    public string[] Categories { get; set; } = [];
}