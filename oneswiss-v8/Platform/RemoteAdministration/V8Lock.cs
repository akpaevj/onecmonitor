using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Lock
{
    [Key(0)] [RacField("connection")] public V8Connection? Connection { get; set; }

    [Key(1)] [RacField("session")] public V8Session? Session { get; set; }

    [Key(2)] [RacField("object")] public string Object { get; set; } = string.Empty;

    [Key(3)] [RacField("locked")] public DateTime Locked { get; set; }

    [Key(4)] [RacField("descr")] public string Descr { get; set; } = string.Empty;
}
