using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceTaskLogItemDto
{
    [Key(0)] 
    public Guid Id { get; set; }
    [Key(1)] 
    public DateTime TimeStamp { get; set; }
    [Key(2)] 
    public bool IsError { get; set; }
    [Key(3)]
    public bool IsFinish { get; set; }
    [Key(4)] 
    public string Message { get; set; } = string.Empty;
    [Key(5)] 
    public Guid? InfoBaseId { get; set; }
    [Key(6)] 
    public Guid? StepId { get; set; }
    [Key(7)] 
    public Guid TaskId { get; set; }
}