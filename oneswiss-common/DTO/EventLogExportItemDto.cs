using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class EventLogExportItemDto
{
    [Key(0)] 
    public InfoBaseDto InfoBase { get; set; }
    [Key(1)]
    public bool IsActive { get; set; }
    [Key(2)]
    public int Ttl { get; set; }
    [Key(3)]
    public bool ReduceSourceLog { get; set; }
    [Key(4)]
    public int ReduceKeepDays { get; set; }
    [Key(5)]
    public DateTime? LastReducedUpTo { get; set; }
}