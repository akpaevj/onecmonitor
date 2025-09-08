using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8ClusterDetails : V8Cluster
{
    [Key(18)] [RacField("ping-period")] public int PingPeriod { get; set; }

    [Key(19)] [RacField("ping-timeout")] public int PingTimeout { get; set; }
}