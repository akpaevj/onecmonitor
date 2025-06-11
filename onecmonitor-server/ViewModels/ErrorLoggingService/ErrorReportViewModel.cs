using OnecMonitor.Server.Dto.ErrorLoggingService;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.ErrorLoggingService;

public class ErrorReportViewModel
{
    public ReportRoot Report { get; set; }
    public string? Screenshot { get; set; } 
}