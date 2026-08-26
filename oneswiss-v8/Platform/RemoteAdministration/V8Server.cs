using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Server
{
    [Key(0)] [RacField("server")] public string Id { get; set; } = string.Empty;

    [Key(1)] [RacField("agent-host")] public string AgentHost { get; set; } = string.Empty;

    [Key(2)] [RacField("agent-port")] public int AgentPort { get; set; }

    [Key(3)] [RacField("port-range")] public string PortRange { get; set; } = string.Empty;

    [Key(4)] [RacField("name")] public string Name { get; set; } = string.Empty;

    [Key(5)] [RacField("using")] public string Using { get; set; } = string.Empty;

    [Key(6)] [RacField("dedicate-managers")] public string DedicateManagers { get; set; } = string.Empty;

    [Key(7)] [RacField("infobases-limit")] public int InfoBasesLimit { get; set; }

    [Key(8)] [RacField("memory-limit")] public long MemoryLimit { get; set; }

    [Key(9)] [RacField("connections-limit")] public int ConnectionsLimit { get; set; }

    [Key(10)]
    [RacField("safe-working-processes-memory-limit")]
    public long SafeWorkingProcessesMemoryLimit { get; set; }

    [Key(11)] [RacField("safe-call-memory-limit")] public long SafeCallMemoryLimit { get; set; }

    [Key(12)] [RacField("cluster-port")] public int ClusterPort { get; set; }

    [Key(13)] [RacField("critical-total-memory")] public long CriticalTotalMemory { get; set; }

    [Key(14)]
    [RacField("temporary-allowed-total-memory")]
    public long TemporaryAllowedTotalMemory { get; set; }

    [Key(15)]
    [RacField("temporary-allowed-total-memory-time-limit")]
    public int TemporaryAllowedTotalMemoryTimeLimit { get; set; }

    [Key(16)]
    [RacField("service-principal-name")]
    public string ServicePrincipalName { get; set; } = string.Empty;

    [Key(17)] [RacField("restart-schedule")] public string RestartSchedule { get; set; } = string.Empty;
}
