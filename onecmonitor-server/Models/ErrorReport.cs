namespace OnecMonitor.Server.Models;

public class ErrorReport : DatabaseObject
{
    public DateTime Date { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public string ConfigurationVersion { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Body { get; set; } = null!;
    public byte[] Screenshot { get; set; } = null!;
}