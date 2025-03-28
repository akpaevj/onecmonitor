using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class ClusterDto
{
    [Key(0)] 
    public string Id { get; set; } = string.Empty;
    [Key(1)] 
    public string ClusterInternalId { get; set; } = string.Empty;
    [Key(2)]
    public string Host { get; set; } = string.Empty;
    [Key(3)] 
    public int Port { get; set; } = 1540;
    [Key(4)]
    public CredentialsDto? Credentials { get; set; }
}