namespace OnecMonitor.Server.ViewModels.Configurations;

public class ConfigurationListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsExtension { get; set; }
}