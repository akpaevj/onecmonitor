using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class ClusterDto
{
    [Key(0)] 
    public string Id { get; set; } = string.Empty;
    [Key(1)]
    public string Host { get; set; } = string.Empty;
    [Key(2)] 
    public int Port { get; set; } = 1540;
}