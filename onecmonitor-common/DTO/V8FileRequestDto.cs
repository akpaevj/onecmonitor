using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class V8FileRequestDto
{
    [Key(0)]
    public Guid Id { get; set; }
}