using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class CloseV8SessionsRequestDto
{
    [Key(0)] 
    public ClusterDto Cluster { get; set; } = null!;

    [Key(1)] 
    public List<string> SessionsIds { get; set; } = [];
}