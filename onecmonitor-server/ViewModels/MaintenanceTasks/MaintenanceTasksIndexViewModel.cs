using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTasksIndexViewModel
{
    public bool ShowArchived { get; set; } = false;
    public bool IsTemplate { get; set; }
    public List<MaintenanceTaskListItemViewModel> Items { get; set; } = [];
    public List<SelectableItemViewModel> Templates { get; set; } = [];
}