using MessagePack;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class CrServerServiceDto
{
    [Key(0)] public CrServer CrServer { get; set; } = null!;

    [Key(1)] public Dictionary<string, Guid> InternalIds { get; set; } = [];
}