using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Edt;

[MessagePackObject]
[ContextClass("ИнсталляцияEDT", "EdtInstallation")]
public class EdtInstallation
{
    [Key(0)]
    [ContextProperty("Версия", "Version", CanWrite = false)]
    public string Version { get; set; }

    [Key(1)]
    [ContextProperty("Путь", "Path", CanWrite = false)]
    public string Path { get; set; }

    [Key(2)]
    [ContextProperty("СуществуетEdtCli", "ExistsEdtCli", CanWrite = false)]
    public bool HasEdtCli { get; set; }

    [Key(3)]
    [ContextProperty("ПутьEdtCli", "PathEdtCli", CanWrite = false)]
    public string EdtCliPath { get; set; }

    [Key(4)]
    [ContextProperty("ИзСтартера", "FromStarter", CanWrite = false)]
    public bool FromStarter { get; set; }
}