using OnecMonitor.Server.Models;
using OnecMonitor.Server.Models.MaintenanceTasks;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTaskLogViewModel
{
    public Guid TaskId { get; set; }
    public Dictionary<InfoBase, List<MaintenanceStepLogItem>> Items  { get; set; } = [];
}