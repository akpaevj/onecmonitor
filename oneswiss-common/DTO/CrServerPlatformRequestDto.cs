using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class CrServerPlatformRequestDto
{
    [Key(0)] public Guid AgentId { get; set; }

    [Key(1)] public int Port { get; set; }
}