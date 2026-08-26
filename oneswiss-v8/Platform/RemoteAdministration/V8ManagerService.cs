using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8ManagerService
{
    [Key(0)] [RacField("name")] public string Name { get; set; } = string.Empty;

    [Key(1)] [RacField("main-only")] public bool MainOnly { get; set; }

    [Key(2)] [RacField("manager")] public string ManagerId { get; set; } = string.Empty;

    [Key(3)] [RacField("descr")] public string Descr { get; set; } = string.Empty;
}
