using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class EventLogReductionResultDto
{
    [Key(0)]
    public Guid InfoBaseId { get; set; }

    [Key(1)]
    public bool Success { get; set; }

    [Key(2)]
    public DateTime? ReducedUpTo { get; set; }

    [Key(3)]
    public string ErrorMessage { get; set; } = string.Empty;
}
