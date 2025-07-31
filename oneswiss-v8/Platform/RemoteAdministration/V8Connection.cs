using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Connection
{
    [Key(0)]
    [RacField("connection")]
    public string Id { get; set; }

    [Key(1)]
    [RacField("conn-id")]
    public int ConnId { get; set; }

    [Key(2)]
    [RacField("host")]
    public string Host { get; set; }

    [Key(3)]
    [RacField("process")]
    public V8Process Process { get; set; }

    [Key(4)]
    [RacField("infobase")]
    public V8InfoBase InfoBase { get; set; }

    [Key(5)]
    [RacField("application")]
    public string Application { get; set; }

    [Key(6)]
    [RacField("connected-at")]
    public DateTime ConnectedAt { get; set; }

    [Key(7)]
    [RacField("session-number")]
    public int SessionNumber { get; set; }

    [Key(8)]
    [RacField("blocked-by-ls")]
    public int BlockedByLs { get; set; }
}