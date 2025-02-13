using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class InfoBaseDto
{
    [Key(0)]
    public Guid Id { get; set; }
    [Key(1)] 
    public string InfoBaseInternalId { get; set; } = string.Empty;
    [Key(2)]
    public string InfoBaseName { get; set; } = string.Empty;
    [Key(3)] 
    public CredentialsDto Credentials { get; set; } = null!;
    [Key(4)] 
    public string PublishAddress { get; set; } = string.Empty;
    [Key(5)] 
    public required ClusterDto Cluster { get; set; }
}