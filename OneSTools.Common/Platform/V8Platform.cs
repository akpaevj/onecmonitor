using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

[DisplayName("Платформа 1С")]
[MessagePackObject]
public class V8Platform
{
    [Key(0)]
    public string PlatformPath { get; init; } = string.Empty;
    [DisplayName("1Cv8 установлена")]
    [Key(1)]
    public bool HasOnecV8 { get; init; }
    [DisplayName("Путь к 1Cv8")]
    [Key(2)]
    public string OnecV8Path { get; init; } = string.Empty;
    [DisplayName("RAC установлен")]
    [Key(3)]
    public bool HasRac { get; init; }
    [DisplayName("Путь к RAC")]
    [Key(4)]
    public string RacPath { get; init; } = string.Empty;
    [DisplayName("RAS установлен")]
    [Key(5)]
    public bool HasRas { get; init; }
    [DisplayName("Путь к RAS")]
    [Key(6)]
    public string RasPath { get; init; } = string.Empty;
    [Key(7)]
    public bool HasIbcmd { get; init; }
    [DisplayName("Путь к ibcmd")]
    [Key(8)]
    public string IbcmdPath { get; init; } = string.Empty;
    [DisplayName("Версия")]
    [Key(9)]
    public string Version { get; init; } = string.Empty;
    
    public override bool Equals(object? obj)
        => obj is V8Platform platform &&
           PlatformPath.ToUpper().Equals(platform.PlatformPath.ToUpper());

    public override int GetHashCode()
        => HashCode.Combine(PlatformPath.ToUpper());

    public override string ToString()
        => $"{Version}";
}