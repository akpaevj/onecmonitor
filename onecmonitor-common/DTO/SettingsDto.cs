using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class SettingsDto
{
    [Key(0)] public bool TechLogEnabled { get; set; } = false;
}