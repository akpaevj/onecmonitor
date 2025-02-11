using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class InfoBaseDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)]
    public Guid InfobaseInternalId { get; set; }
    [Key(2)]
    public string InfoBaseName { get; set; } = string.Empty;
    [Key(3)] 
    public CredentialsDto Credentials { get; set; } = null!;
    [Key(4)] 
    public string PublishAddress { get; set; } = string.Empty;
    [Key(5)] 
    public required ClusterDto ClusterDto { get; set; }
}