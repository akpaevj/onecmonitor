using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class SettingsDto
{
    [Key(0)] public bool TechLogEnabled { get; set; } = false;

    [Key(1)] public EventLogSettingsDto EventLogSettings { get; set; } = null!;

    [Key(2)] public TechLogSettingsDto TechLogSettings { get; set; } = null!;
}