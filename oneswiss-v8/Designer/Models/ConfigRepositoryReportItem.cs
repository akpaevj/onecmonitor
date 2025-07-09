namespace OneSwiss.V8.Designer.Models;

public class ConfigRepositoryReportItem
{
    public int Version { get; internal set; } = 0;
    public string ConfigurationVersion { get; internal set; } = string.Empty;
    public string User { get; internal set; } = string.Empty;
    public DateTime CreatedAt { get; internal set; } = DateTime.MinValue;
    public string Comment { get; internal set; } = string.Empty;
    
    public IReadOnlyList<string> Added { get; internal set; } = new List<string>();
    public IReadOnlyList<string> Changed { get; internal set; } = new List<string>();
    public IReadOnlyList<string> Deleted { get; internal set; } = new List<string>();
}