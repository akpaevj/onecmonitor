using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class V8FileChunkDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)] 
    public byte[] Data { get; set; } = [];
}