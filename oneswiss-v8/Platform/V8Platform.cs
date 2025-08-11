using System.ComponentModel;
using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Platform;

[DisplayName("Платформа 1С")]
[ContextClass("ПлатформаV8", "PlatformV8")]
[MessagePackObject]
public class V8Platform
{
    [Key(0)]
    [ContextProperty("Путь", "Path", CanWrite = false)]
    public string PlatformPath { get; init; } = string.Empty;
    [DisplayName("1Cv8 установлена")]
    [Key(1)]
    [ContextProperty("Существует1CV8", "Exists1CV8", CanWrite = false)]
    public bool HasOnecV8 { get; init; }
    [DisplayName("Путь к 1Cv8")]
    [Key(2)]
    [ContextProperty("Путь1CV8", "Path1CV8", CanWrite = false)]
    public string OnecV8Path { get; init; } = string.Empty;
    [DisplayName("RAC установлен")]
    [Key(3)]
    [ContextProperty("СуществуетRac", "ExistsRac", CanWrite = false)]
    public bool HasRac { get; init; }
    [DisplayName("Путь к RAC")]
    [Key(4)]
    [ContextProperty("ПутьRac", "PathRac", CanWrite = false)]
    public string RacPath { get; init; } = string.Empty;
    [DisplayName("RAS установлен")]
    [Key(5)]
    [ContextProperty("СуществуетRas", "ExistsRas", CanWrite = false)]
    public bool HasRas { get; init; }
    [DisplayName("Путь к RAS")]
    [Key(6)]
    [ContextProperty("ПутьRas", "PathRas", CanWrite = false)]
    public string RasPath { get; init; } = string.Empty;
    [Key(7)]
    [ContextProperty("СуществуетIbcmd", "ExistsIbcmd", CanWrite = false)]
    public bool HasIbcmd { get; init; }
    [DisplayName("Путь к ibcmd")]
    [Key(8)]
    [ContextProperty("ПутьIbcmd", "PathIbcmd", CanWrite = false)]
    public string IbcmdPath { get; init; } = string.Empty;
    [DisplayName("Версия")]
    [Key(9)]
    [ContextProperty("Версия", "Version", CanWrite = false)]
    public string Version { get; init; } = string.Empty;
    
    public override bool Equals(object? obj)
        => obj is V8Platform platform &&
           PlatformPath.ToUpper().Equals(platform.PlatformPath.ToUpper());

    public override int GetHashCode()
        => HashCode.Combine(PlatformPath.ToUpper());

    public override string ToString()
        => $"{Version}";
}