using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Cluster
{
    [Key(0)]
    [RacField("cluster")]
    public string Id { get; set; }
    [Key(1)]
    [RacField("name")]
    public string Name { get; set; }
    [Key(2)]
    [RacField("host")]
    public string Host { get; set; }
    [Key(4)]
    [RacField("port")]
    public int Port { get; set; }
    [Key(5)]
    [RacField("expiration-timeout")]
    public int ExpirationTimeout { get; set; }
    [Key(6)]
    [RacField("lifetime-limit")]
    public int LifetimeLimit { get; set; }
    [Key(7)]
    [RacField("max-memory-size")]
    public int MaxMemorySize { get; set; }
    [Key(8)]
    [RacField("max-memory-time-limit")]
    public int MaxMemoryTimeLimit { get; set; }
    [Key(9)]
    [RacField("security-level")]
    public V8SecurityLevel SecurityLevel { get; set; }
    [Key(10)]
    [RacField("session-fault-tolerance-level")]
    public int SessionFaultToleranceLevel { get; set; }
    [Key(11)]
    [RacField("load-balancing-mode")]
    public V8ClusterLoadBalancingMode LoadBalancingMode { get; set; }
    [Key(12)]
    [RacField("errors-count-threshold")]
    public int ErrorCountThreshold { get; set; }
    [Key(13)]
    [RacField("kill-problem-processes", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool KillProblemProcesses { get; set; }
    [Key(14)]
    [RacField("kill-by-memory-with-dump", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool KillByMemoryWithDump { get; set; }
    [Key(15)]
    [RacField("allow-access-right-audit-events-recording", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool AllowAccessRightAuditEventsRecording { get; set; }
    [Key(16)]
    [RacField("restart-schedule")]
    public string RestartSchedule { get; set; }
    [Key(17)]
    public int RagentPort { get; set; }
}