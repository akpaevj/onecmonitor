using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class V8ConnectionsRequestDto
{
    [Key(0)] public ClusterDto Cluster { get; set; } = null!;

    [Key(1)] public InfoBaseDto? InfoBase { get; set; }
}
