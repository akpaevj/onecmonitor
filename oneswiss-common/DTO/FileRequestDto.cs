using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class FileRequestDto
{
    [Key(0)] public Guid Id { get; set; }
}