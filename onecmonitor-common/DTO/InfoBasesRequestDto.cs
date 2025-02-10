using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class InfoBasesRequestDto
{
    [Key(0)]
    public string ClusterId { get; set; }
}