namespace OnecMonitor.Server.ViewModels.UpdateInfoBaseTasks;

public class UpdateInfoBaseTaskListItemViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsStarted { get; set; } = false;
    public bool IsFinished { get; set; } = false;
    public bool IsFaulted { get; set; } = false;
}