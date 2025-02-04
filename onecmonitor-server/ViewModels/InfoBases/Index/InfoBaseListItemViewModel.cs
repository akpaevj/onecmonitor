namespace OnecMonitor.Server.ViewModels.InfoBases.Index;

public class InfoBaseListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string PublishAddress { get; set; } = string.Empty;
    public string AdminUser { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}