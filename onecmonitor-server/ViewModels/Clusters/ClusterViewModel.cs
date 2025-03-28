using System.ComponentModel;
using OnecMonitor.Server.ViewModels.Agents;

namespace OnecMonitor.Server.ViewModels.Clusters;

[DisplayName("Кластер")]
public class ClusterViewModel
{
    public Guid Id { get; set; }
    [DisplayName("Имя")]
    public string Name { get; set; } = string.Empty;
    [DisplayName("Агент монитора")]
    public AgentViewModel Agent { get; set; } = null!;
}