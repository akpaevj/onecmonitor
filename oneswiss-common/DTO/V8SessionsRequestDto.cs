using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class V8SessionsRequestDto
{
    [Key(0)]
    public ClusterDto Cluster { get; set; }
    [Key(1)]
    public InfoBaseDto? InfoBase { get; set; }
}