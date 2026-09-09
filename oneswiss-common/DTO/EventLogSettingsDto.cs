using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class EventLogSettingsDto
{
    [Key(0)] 
    public bool Enabled { get; set; }

    [Key(1)] 
    public DbmsDto Dbms { get; set; }

    [Key(2)] 
    public string DatabaseName { get; set; }

    [Key(3)] 
    public string Table { get; set; } = string.Empty;

    [Key(4)]
    public CredentialsDto Credentials { get; set; }

    [Key(5)]
    public List<EventLogExportItemDto> Items { get; set; } = [];

    [Key(6)]
    public bool ReductionEnabled { get; set; }

    [Key(7)]
    public int ReductionHourUtc { get; set; }

    [Key(8)]
    public int ReductionSafetyMarginHours { get; set; }
}