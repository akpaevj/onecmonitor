using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTaskLogViewModel
{
    public Guid TaskId { get; set; }
    public List<InfoBase> InfoBases  { get; set; } = [];
}