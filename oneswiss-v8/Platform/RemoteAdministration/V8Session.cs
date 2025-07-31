using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Session
{
    [Key(0)]
    [RacField("session")]
    public string Id { get; set; } = string.Empty;
    [Key(1)]
    [RacField("session-id")]
    public string SessionId { get; set; } = string.Empty;
    [Key(2)]
    [RacField("infobase")]
    public V8InfoBase InfoBase { get; set; } = null!;
    [Key(3)]
    [RacField("connection")]
    public V8Connection Connection { get; set; } = null!;
    [Key(4)]
    [RacField("process")]
    public V8Process Process { get; set; } = null!;
    [Key(5)]
    [RacField("user-name")]
    public string UserName { get; set; }
    [Key(6)]
    [RacField("host")]
    public string Host { get; set; }
    [Key(7)]
    [RacField("app-id")]
    public string AppId { get; set; }
    [Key(8)]
    [RacField("locale")]
    public string Locale { get; set; }
    [Key(9)]
    [RacField("started-at")]
    public DateTime StartedAt { get; set; }
    [Key(10)]
    [RacField("last-active-at")]
    public DateTime LastActiveAt { get; set; }
    [Key(11)]
    [RacField("hibernate", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool Hibernate { get; set; }
    [Key(12)]
    [RacField("passive-session-hibernate-time")]
    public int PassiveSessionHibernateTime { get; set; }
    [Key(13)]
    [RacField("hibernate-session-terminate-time")]
    public int HibernateSessionTerminateTime { get; set; }
    [Key(14)]
    [RacField("blocked-by-dbms")]
    public long BlockedByDbms { get; set; }
    [Key(15)]
    [RacField("blocked-by-ls")]
    public long BlockedByLs { get; set; }
    [Key(16)]
    [RacField("bytes-all")]
    public long BytesAll { get; set; }
    [Key(17)]
    [RacField("bytes-last-5min")]
    public long BytesLast5Min { get; set; }
    [Key(18)]
    [RacField("calls-all")]
    public long CallsAll { get; set; }
    [Key(19)]
    [RacField("calls-last-5min")]
    public long CallsLast5Min { get; set; }
    [Key(20)]
    [RacField("dbms-bytes-all")]
    public long DbmsBytesAll { get; set; }
    [Key(21)]
    [RacField("dbms-bytes-last-5min")]
    public long DbmsBytesLast5Min { get; set; }
    [Key(22)]
    [RacField("db-proc-info")]
    public string DbProcInfo { get; set; }
    [Key(23)]
    [RacField("db-proc-took")]
    public long DbProcTook { get; set; }
    [Key(24)]
    [RacField("db-proc-took-at")]
    public string DbProcTookAt { get; set; }
    [Key(25)]
    [RacField("duration-all")]
    public long DurationAll { get; set; }
    [Key(26)]
    [RacField("duration-all-dbms")]
    public long DurationAllDbms { get; set; }
    [Key(27)]
    [RacField("duration-current")]
    public long DurationCurrent { get; set; }
    [Key(28)]
    [RacField("duration-current-dbms")]
    public long DurationCurrentDbms { get; set; }
    [Key(29)]
    [RacField("duration-last-5min")]
    public long DurationLast5Min { get; set; }
    [Key(30)]
    [RacField("duration-last-5min-dbms")]
    public long DurationLast5MinDbms { get; set; }
    [Key(31)]
    [RacField("memory-current")]
    public long MemoryCurrent { get; set; }
    [Key(32)]
    [RacField("memory-last-5min")]
    public long MemoryLast5Min { get; set; }
    [Key(33)]
    [RacField("memory-total")]
    public long MemoryTotal { get; set; }
    [Key(34)]
    [RacField("read-current")]
    public long ReadCurrent { get; set; }
    [Key(35)]
    [RacField("read-last-5min")]
    public long ReadLast5Min { get; set; }
    [Key(36)]
    [RacField("read-total")]
    public long ReadTotal { get; set; }
    [Key(37)]
    [RacField("write-current")]
    public long WriteCurrent { get; set; }
    [Key(38)]
    [RacField("write-last-5min")]
    public long WriteLast5Min { get; set; }
    [Key(39)]
    [RacField("write-total")]
    public long WriteTotal { get; set; }
    [Key(40)]
    [RacField("duration-current-service")]
    public long DurationCurrentService { get; set; }
    [Key(41)]
    [RacField("duration-last-5min-service")]
    public long DurationLast5MinService { get; set; }
    [Key(42)]
    [RacField("duration-all-service")]
    public long DurationAllService { get; set; }
    [Key(43)]
    [RacField("current-service-name")]
    public string CurrentServiceName { get; set; }
    [Key(44)]
    [RacField("cpu-time-current")]
    public long CpuTimeCurrent { get; set; }
    [Key(45)]
    [RacField("cpu-time-last-5min")]
    public long CpuTimeLast5Min { get; set; }
    [Key(46)]
    [RacField("cpu-time-total")]
    public long CpuTimeTotal { get; set; }
    [Key(47)]
    [RacField("data-separation")]
    public string DataSeparation { get; set; }
    [Key(48)]
    [RacField("client-ip")]
    public string ClientIp { get; set; }
}