using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class InfoBaseDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)]
    public string InfoBaseName { get; set; } = string.Empty;
    [Key(2)] 
    public string AdminUser { get; set; } = string.Empty;
    [Key(3)] 
    public string AdminPassword { get; set; } = string.Empty;
    [Key(4)] 
    public string PublishAddress { get; set; } = string.Empty;
    [Key(5)] 
    public required ClusterDto ClusterDto { get; set; }
}