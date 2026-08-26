using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Manager
{
    [Key(0)] [RacField("manager")] public string Id { get; set; } = string.Empty;

    [Key(1)] [RacField("pid")] public int Pid { get; set; }

    [Key(2)] [RacField("using")] public string Using { get; set; } = string.Empty;

    [Key(3)] [RacField("host")] public string Host { get; set; } = string.Empty;

    [Key(4)] [RacField("port")] public int Port { get; set; }

    [Key(5)] [RacField("descr")] public string Descr { get; set; } = string.Empty;
}
