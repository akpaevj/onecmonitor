using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8BinaryDataStorage
{
    [Key(0)] [RacField("storage")] public string Id { get; set; } = string.Empty;

    [Key(1)] [RacField("name")] public string Name { get; set; } = string.Empty;
}
