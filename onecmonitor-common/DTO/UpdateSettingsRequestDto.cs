using MessagePack;

namespace OnecMonitor.Common.DTO;

[MessagePackObject]
public class UpdateSettingsRequestDto
{
    [Key(0)] public bool TechLogEnabled { get; set; } = false;
}