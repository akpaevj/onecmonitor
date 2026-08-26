using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class V8ServiceSettingsRequestDto
{
    [Key(0)] public ClusterDto Cluster { get; set; } = null!;

    [Key(1)] public string ServerId { get; set; } = string.Empty;
}
