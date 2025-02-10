namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTaskResult : DatabaseObject
{
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;
    public bool IsFaulted { get; set; } = false;
    
    public Guid UpdateInfoBaseTaskId { get; set; }
    public UpdateInfoBaseTask Task { get; set; }
    
    public Guid InfoBaseId { get; set; }
    public InfoBase InfoBase { get; set; }
    
    public string Log { get; set; } = string.Empty;
}