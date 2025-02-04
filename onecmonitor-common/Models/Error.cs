using MessagePack;

namespace OnecMonitor.Common.Models;

[MessagePackObject]
public class Error
{
    [Key(0)]
    public string Message { get; set; } = string.Empty;
}