using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class StreamDescription
{
    [Key(1)] public long Length { get; set; }
}