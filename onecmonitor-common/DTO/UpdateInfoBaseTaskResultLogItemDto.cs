using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class UpdateInfoBaseTaskResultLogItemDto
{
    [Key(0)] 
    public DateTime TimeStamp { get; set; }
    [Key(1)] 
    public bool IsError { get; set; }
    [Key(2)] 
    public string Message { get; set; } = string.Empty;

    public static UpdateInfoBaseTaskResultLogItemDto Create(string message, bool isError = false)
        => new UpdateInfoBaseTaskResultLogItemDto
        {
            TimeStamp = DateTime.Now,
            IsError = isError,
            Message = message
        };
}