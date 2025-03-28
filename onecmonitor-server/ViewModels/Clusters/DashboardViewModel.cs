using System.ComponentModel;
using OnecMonitor.Server.ViewModels.InfoBases;

namespace OnecMonitor.Server.ViewModels.Clusters;

[DisplayName("Кластеры и информационные базы")]
public class DashboardViewModel
{
    [DisplayName("Кластеры")]
    public List<ClusterViewModel> Clusters { get; set; } = [];
    [DisplayName("Информационные базы кластера")]
    public List<InfoBaseViewModel> InfoBases { get; set; } = [];
}