using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8License
{
    [Key(0)] [RacField("process")] public string? ProcessId { get; set; }

    [Key(1)] [RacField("session")] public string? SessionId { get; set; }

    [Key(2)] [RacField("host")] public string Host { get; set; } = string.Empty;

    [Key(3)] [RacField("port")] public int Port { get; set; }

    [Key(4)] [RacField("pid")] public int Pid { get; set; }

    [Key(5)] [RacField("full-name")] public string FullName { get; set; } = string.Empty;

    [Key(6)] [RacField("series")] public string Series { get; set; } = string.Empty;

    [Key(7)] [RacField("issued-by-server")] public bool IssuedByServer { get; set; }

    [Key(8)] [RacField("license-type")] public string LicenseType { get; set; } = string.Empty;

    [Key(9)] [RacField("net")] public bool Net { get; set; }

    [Key(10)] [RacField("max-users-all")] public int MaxUsersAll { get; set; }

    [Key(11)] [RacField("max-users-cur")] public int MaxUsersCur { get; set; }

    [Key(12)] [RacField("rmngr-address")] public string RmngrAddress { get; set; } = string.Empty;

    [Key(13)] [RacField("rmngr-port")] public int RmngrPort { get; set; }

    [Key(14)] [RacField("rmngr-pid")] public int RmngrPid { get; set; }

    [Key(15)]
    [RacField("short-presentation")]
    public string ShortPresentation { get; set; } = string.Empty;

    [Key(16)]
    [RacField("full-presentation")]
    public string FullPresentation { get; set; } = string.Empty;

    /// <summary>
    /// Set locally after fetching - "process" or "session", depending on which RAC command
    /// ("process list --licenses" or "session list --licenses") produced this record.
    /// </summary>
    [Key(17)]
    public string Source { get; set; } = string.Empty;
}
