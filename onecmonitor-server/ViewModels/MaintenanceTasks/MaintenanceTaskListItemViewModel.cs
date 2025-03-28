namespace OnecMonitor.Server.ViewModels.MaintenanceTasks;

public class MaintenanceTaskListItemViewModel
{
    public Guid Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime FinishDateTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsFaulted { get; set; } = false;
    public bool IsArchived { get; set; } = false;
}