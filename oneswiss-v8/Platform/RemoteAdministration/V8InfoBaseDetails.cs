using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8InfoBaseDetails : V8InfoBase
{
    [Key(3)] [RacField("dbms")] public V8InfoBaseDbms Dbms { get; set; }

    [Key(4)] [RacField("db-server")] public string DbServer { get; set; }

    [Key(5)] [RacField("db-name")] public string DbName { get; set; }

    [Key(6)] [RacField("db-user")] public string DbUser { get; set; }

    [Key(7)] [RacField("security-level")] public V8SecurityLevel SecurityLevel { get; set; }

    [Key(8)]
    [RacField("license-distribution", TrueFalseForm = RacFieldTrueFalseForm.AllowDeny)]
    public bool LicenseDistribution { get; set; }

    [Key(9)]
    [RacField("scheduled-jobs-deny", TrueFalseForm = RacFieldTrueFalseForm.OnOff)]
    public bool ScheduledJobsDeny { get; set; }

    [Key(10)]
    [RacField("sessions-deny", TrueFalseForm = RacFieldTrueFalseForm.OnOff)]
    public bool SessionsDeny { get; set; }

    [Key(11)] [RacField("denied-from")] public string DeniedFrom { get; set; }

    [Key(12)] [RacField("denied-message")] public string DeniedMessage { get; set; }

    [Key(13)]
    [RacField("denied-parameter")]
    public string DeniedParameter { get; set; }

    [Key(14)] [RacField("denied-to")] public string DeniedTo { get; set; }

    [Key(15)]
    [RacField("permission-code")]
    public string PermissionCode { get; set; }

    [Key(16)]
    [RacField("external-session-manager-connection-string")]
    public string ExternalSessionManagerConnectionString { get; set; }

    [Key(17)]
    [RacField("external-session-manager-required", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool ExternalSessionManagerRequired { get; set; }

    [Key(18)]
    [RacField("security-profile-name")]
    public string SecurityProfileName { get; set; }

    [Key(19)]
    [RacField("safe-mode-security-profile-name")]
    public string SafeModeSecurityProfileName { get; set; }

    [Key(20)]
    [RacField("reserve-working-processes", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool ReserveWorkingProcesses { get; set; }

    [Key(21)]
    [RacField("disable-local-speech-to-text", TrueFalseForm = RacFieldTrueFalseForm.YesNo)]
    public bool DisableLocalSpeechToText { get; set; }

    [Key(22)]
    [RacField("configuration-unload-delay-by-working-process-without-active-users")]
    public int ConfigurationUnloadDelayByWorkingProcessWithoutActiveUsers { get; set; }

    [Key(23)]
    [RacField("minimum-scheduled-jobs-start-period-without-active-users")]
    public int MinimumScheduledJobsStartPeriodWithoutActiveUsers { get; set; }

    [Key(24)]
    [RacField("maximum-scheduled-jobs-start-shift-without-active-users")]
    public int MaximumScheduledJobsStartShiftWithoutActiveUsers { get; set; }

    [Key(25)] [RacField("db-pwd")] public string DbPwd { get; set; }
}