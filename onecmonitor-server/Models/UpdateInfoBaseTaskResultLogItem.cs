namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTaskResultLogItem : DatabaseObject
{
    public DateTime TimeStamp { get; set; }
    public bool IsError { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid UpdateInfoBaseTaskResultId { get; set; }
    
    public virtual UpdateInfoBaseTaskResult UpdateInfoBaseTaskResult { get; set; } = null!;
}