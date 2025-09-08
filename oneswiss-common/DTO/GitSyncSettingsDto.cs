using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class GitSyncSettingsDto
{
    [Key(0)] public bool Enabled { get; set; }
}