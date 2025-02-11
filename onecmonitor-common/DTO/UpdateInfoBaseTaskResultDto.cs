using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class UpdateInfoBaseTaskResultDto
{
    [Key(0)]
    public bool IsFaulted { get; set; } = false;
    [Key(1)]
    public Guid TaskId { get; set; }
    [Key(2)]
    public Guid InfoBaseId { get; set; }
    [Key(3)] 
    public List<UpdateInfoBaseTaskResultLogItemDto> Log { get; set; } = [];
}