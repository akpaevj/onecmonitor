using System.ComponentModel;
using OnecMonitor.Server.ViewModels.Clusters;

namespace OnecMonitor.Server.ViewModels.InfoBases;

[DisplayName("Информационная база")]
public class InfoBaseViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Имя")]
    public string Name { get; set; } = string.Empty;
    [DisplayName("Кластер")]
    public ClusterViewModel Cluster { get; set; } = null!;
}