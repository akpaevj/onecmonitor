using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8ServiceSetting
{
    [Key(0)] [RacField("setting")] public string Id { get; set; } = string.Empty;

    [Key(1)] [RacField("service-name")] public string ServiceName { get; set; } = string.Empty;

    [Key(2)] [RacField("infobase-name")] public string InfoBaseName { get; set; } = string.Empty;

    [Key(3)] [RacField("service-data-dir")] public string ServiceDataDir { get; set; } = string.Empty;
}
