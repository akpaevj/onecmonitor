using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

[DisplayName("Платформа 1С")]
[MessagePackObject]
public record V8Platform
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
    [DisplayName("Версия")]
    [Key(6)]
    public string Version { get; init; } = string.Empty;
}