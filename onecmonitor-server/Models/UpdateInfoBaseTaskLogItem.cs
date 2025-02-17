using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTaskLogItem : DatabaseObject
{
    [DataType(DataType.Date)]
    public DateTime TimeStamp { get; set; }
    public bool IsError { get; set; }
    public bool IsFinish { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public Guid InfoBaseId { get; set; }
    public Guid TaskId { get; set; } 
    
    public virtual InfoBase InfoBase { get; set; } = null!;
    public virtual UpdateInfoBaseTask Task { get; set; } = null!;
}