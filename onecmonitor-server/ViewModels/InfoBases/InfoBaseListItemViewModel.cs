namespace OnecMonitor.Server.ViewModels.InfoBases;

public class InfoBaseListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InfoBaseName { get; set; } = string.Empty;
    public string Cluster { get; set; } = string.Empty;
}