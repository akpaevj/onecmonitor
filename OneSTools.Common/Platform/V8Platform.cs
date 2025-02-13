using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

[DisplayName("Платформа 1С")]
[MessagePackObject]
public class V8Platform
{
    [DisplayName("Архитектура")]
    [Key(0)]
    public Arch Arch { get; init; }
    [Key(1)]
    public string PlatformPath { get; init; } = string.Empty;
    [DisplayName("1Cv8 установлена")]
    [Key(2)]
    public bool HasOnecV8 { get; init; }
    [DisplayName("Путь к 1Cv8")]
    [Key(3)]
    public string OnecV8Path { get; init; } = string.Empty;
    [DisplayName("RAC установлен")]
    [Key(4)]
    public bool HasRac { get; init; }
    [DisplayName("Путь к RAC")]
    [Key(5)]
    public string RacPath { get; init; } = string.Empty;
    [DisplayName("RAS установлен")]
    [Key(6)]
    public bool HasRas { get; init; }
    [DisplayName("Путь к RAS")]
    [Key(7)]
    public string RasPath { get; init; } = string.Empty;
    [DisplayName("Версия")]
    [Key(8)]
    public string Version { get; init; } = string.Empty;
    
    public override bool Equals(object? obj)
        => obj is V8Platform platform &&
           PlatformPath.ToUpper().Equals(platform.PlatformPath.ToUpper());

    public override int GetHashCode()
        => HashCode.Combine(PlatformPath.ToUpper());

    public override string ToString()
        => $"{Version} ({Arch})";
}