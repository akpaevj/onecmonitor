using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class InfoBasesRequestDto
{
    [Key(0)] 
    public ClusterDto Cluster { get; set; } = null!;
    [Key(1)] 
    public CredentialsDto? Credentials { get; set; }
}