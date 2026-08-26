using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8AssignmentRule
{
    [Key(0)] [RacField("rule")] public string Id { get; set; } = string.Empty;

    [Key(1)] [RacField("position")] public int Position { get; set; }

    [Key(2)] [RacField("object-type")] public string ObjectType { get; set; } = string.Empty;

    [Key(3)] [RacField("infobase-name")] public string InfoBaseName { get; set; } = string.Empty;

    [Key(4)] [RacField("rule-type")] public string RuleType { get; set; } = string.Empty;

    [Key(5)] [RacField("application-ext")] public string ApplicationExt { get; set; } = string.Empty;

    [Key(6)] [RacField("priority")] public int Priority { get; set; }
}
