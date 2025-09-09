using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8InfoBase
{
    [Key(0)] [RacField("infobase")] public string Id { get; set; }

    [Key(1)] [RacField("name")] public string Name { get; set; }

    [Key(2)] [RacField("descr")] public string Description { get; set; }
}