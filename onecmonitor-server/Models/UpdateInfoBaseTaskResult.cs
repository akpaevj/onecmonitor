namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTaskResult : DatabaseObject
{
    public DateTime FinishDateTime { get; set; } = DateTime.MinValue;
    public bool IsFaulted { get; set; } = false;
    public Guid UpdateInfoBaseTaskId { get; set; }
    public Guid InfoBaseId { get; set; } 
    
    public virtual List<UpdateInfoBaseTaskResultLogItem> Log { get; set; } = [];
    public virtual UpdateInfoBaseTask UpdateInfoBaseTask { get; set; } = null!;
    public virtual InfoBase InfoBase { get; set; } = null!;
}