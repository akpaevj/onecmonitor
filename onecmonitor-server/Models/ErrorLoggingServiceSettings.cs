namespace OnecMonitor.Server.Models;

public class ErrorLoggingServiceSettings : DatabaseObject
{
    public bool Enabled { get; set; }
    public int ReportsTtl { get; set; }
}