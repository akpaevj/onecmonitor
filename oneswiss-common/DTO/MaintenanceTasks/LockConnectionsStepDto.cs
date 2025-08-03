using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class LockConnectionsStepDto
{
    [Key(0)]
    public string AccessCode { get; set; }
    [Key(1)]
    public string Message { get; set; }
}