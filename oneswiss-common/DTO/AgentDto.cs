using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class AgentDto
{
    [Key(0)] public Guid Id { get; set; } = Guid.Empty;

    [Key(1)] public string InstanceName { get; set; } = string.Empty;
}