using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8InfoBase : V8InfoBaseSummary
{
    [Key(2)]
    public bool SessionsDeny { get; set; } = false;
    [Key(3)]
    public bool ScheduledJobsDeny { get; set; } = false;
    [Key(4)]
    public string DeniedMessage { get; set; } = string.Empty;
    [Key(5)]
    public string PermissionCode { get; set; } = string.Empty;
}