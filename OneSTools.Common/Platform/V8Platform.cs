using MessagePack;

namespace OneSTools.Common.Platform;

[MessagePackObject]
public record V8Platform
{
    [Key(0)]
    public Arch Arch { get; init; }
    [Key(1)]
    public string PlatformPath { get; init; } = string.Empty;
    [Key(2)]
    public bool HasOnecV8 { get; init; }
    [Key(3)]
    public string OnecV8Path { get; init; } = string.Empty;
    [Key(4)]
    public bool HasRac { get; init; }
    [Key(5)]
    public string RacPath { get; init; } = string.Empty;
    [Key(6)]
    public string Version { get; init; } = string.Empty;
}