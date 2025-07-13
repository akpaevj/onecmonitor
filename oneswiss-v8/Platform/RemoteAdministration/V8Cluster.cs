using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8Cluster
{
    [Key(0)]
    public string Id { get; set; }
    [Key(1)]
    public string Name { get; set; }
    [Key(2)]
    public string Host { get; set; }
    [Key(4)]
    public int Port { get; set; }
}