using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8ResourceLimit
{
    [Key(0)] [RacField("name")] public string Name { get; set; } = string.Empty;

    [Key(1)] [RacField("action")] public string Action { get; set; } = string.Empty;

    [Key(2)] [RacField("counter")] public string Counter { get; set; } = string.Empty;

    [Key(3)] [RacField("duration")] public long Duration { get; set; }

    [Key(4)] [RacField("cpu-time")] public long CpuTime { get; set; }

    [Key(5)] [RacField("memory")] public long Memory { get; set; }

    [Key(6)] [RacField("read")] public long Read { get; set; }

    [Key(7)] [RacField("write")] public long Write { get; set; }

    [Key(8)] [RacField("duration-dbms")] public long DurationDbms { get; set; }

    [Key(9)] [RacField("dbms-bytes")] public long DbmsBytes { get; set; }

    [Key(10)] [RacField("service")] public long Service { get; set; }

    [Key(11)] [RacField("call")] public long Call { get; set; }

    [Key(12)]
    [RacField("number-of-active-sessions")]
    public long NumberOfActiveSessions { get; set; }

    [Key(13)] [RacField("number-of-sessions")] public long NumberOfSessions { get; set; }

    [Key(14)] [RacField("error-message")] public string ErrorMessage { get; set; } = string.Empty;

    [Key(15)] [RacField("descr")] public string Descr { get; set; } = string.Empty;
}
