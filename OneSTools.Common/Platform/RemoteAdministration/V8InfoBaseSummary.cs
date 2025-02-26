using MessagePack;

namespace OneSTools.Common.Platform.RemoteAdministration;

[MessagePackObject]
public class V8InfoBaseSummary
{
    [Key(0)]
    public string Id { get; set; }
    [Key(1)]
    public string Name { get; set; }
}