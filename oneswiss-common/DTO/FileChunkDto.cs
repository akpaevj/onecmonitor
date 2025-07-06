using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class FileChunkDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)] 
    public byte[] Data { get; set; } = [];
}