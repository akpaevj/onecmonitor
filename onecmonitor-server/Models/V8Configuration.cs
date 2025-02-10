namespace OnecMonitor.Server.Models;

public class V8Configuration : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsExtension { get; set; } = false;
    public string DataPath { get; set; }  = string.Empty;
}