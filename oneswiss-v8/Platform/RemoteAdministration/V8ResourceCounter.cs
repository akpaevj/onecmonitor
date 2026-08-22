using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8ResourceCounter
{
    [Key(0)] [RacField("name")] public string Name { get; set; } = string.Empty;

    [Key(1)] [RacField("collection-time")] public string CollectionTime { get; set; } = string.Empty;

    [Key(2)] [RacField("group")] public string Group { get; set; } = string.Empty;

    [Key(3)] [RacField("filter-type")] public string FilterType { get; set; } = string.Empty;

    [Key(4)] [RacField("filter")] public string Filter { get; set; } = string.Empty;

    // "duration".."number-of-sessions" hold "analyze"/"not-analyze", not a recognized true/false
    // form (RacField bool parsing only understands "1"/"on"/"yes"/"allow"), so these stay strings
    // to avoid silently mapping every value to false.
    [Key(5)] [RacField("duration")] public string Duration { get; set; } = string.Empty;

    [Key(6)] [RacField("cpu-time")] public string CpuTime { get; set; } = string.Empty;

    [Key(7)] [RacField("memory")] public string Memory { get; set; } = string.Empty;

    [Key(8)] [RacField("read")] public string Read { get; set; } = string.Empty;

    [Key(9)] [RacField("write")] public string Write { get; set; } = string.Empty;

    [Key(10)] [RacField("duration-dbms")] public string DurationDbms { get; set; } = string.Empty;

    [Key(11)] [RacField("dbms-bytes")] public string DbmsBytes { get; set; } = string.Empty;

    [Key(12)] [RacField("service")] public string Service { get; set; } = string.Empty;

    [Key(13)] [RacField("call")] public string Call { get; set; } = string.Empty;

    [Key(14)]
    [RacField("number-of-active-sessions")]
    public string NumberOfActiveSessions { get; set; } = string.Empty;

    [Key(15)]
    [RacField("number-of-sessions")]
    public string NumberOfSessions { get; set; } = string.Empty;

    [Key(16)] [RacField("descr")] public string Descr { get; set; } = string.Empty;
}
