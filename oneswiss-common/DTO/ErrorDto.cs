using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class ErrorDto
{
    [Key(0)]
    public string Message { get; set; } = string.Empty;
}