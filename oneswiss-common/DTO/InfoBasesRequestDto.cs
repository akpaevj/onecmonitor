using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class InfoBasesRequestDto
{
    [Key(0)] public ClusterDto Cluster { get; set; } = null!;
}