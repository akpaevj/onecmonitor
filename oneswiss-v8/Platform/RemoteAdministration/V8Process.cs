using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Process
{
    [Key(0)] [RacField("process")] public string Id { get; set; }

    [Key(1)] [RacField("host")] public string Host { get; set; }

    [Key(2)] [RacField("port")] public int Port { get; set; }

    [Key(3)] [RacField("pid")] public int Pid { get; set; }

    [Key(4)] [RacField("turned-on")] public bool TurnedOn { get; set; }

    [Key(5)] [RacField("running")] public bool Running { get; set; }

    [Key(6)] [RacField("started-at")] public DateTime StartedAt { get; set; }

    [Key(7)] [RacField("use")] public bool Use { get; set; }

    [Key(8)]
    [RacField("available-perfomance")]
    public int AvailablePerfomance { get; set; }

    [Key(9)] [RacField("capacity")] public int Capacity { get; set; }

    [Key(10)] [RacField("connections")] public int Connections { get; set; }

    [Key(11)] [RacField("memory-size")] public int MemorySize { get; set; }

    [Key(12)]
    [RacField("memory-excess-time")]
    public int MemoryExcessTime { get; set; }

    [Key(13)] [RacField("selection-size")] public int SelectionSize { get; set; }

    [Key(14)] [RacField("avg-call-time")] public double AvgCallTime { get; set; }

    [Key(15)]
    [RacField("avg-db-call-time")]
    public double AvgDbCallTime { get; set; }

    [Key(16)]
    [RacField("avg-lock-call-time")]
    public double AvgLockCallTime { get; set; }

    [Key(17)]
    [RacField("avg-server-call-time")]
    public double AvgServerCallTime { get; set; }

    [Key(18)] [RacField("avg-threads")] public double AvgThreads { get; set; }

    [Key(19)] [RacField("reserve")] public bool Reserve { get; set; }
}