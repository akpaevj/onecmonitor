using MessagePack;

namespace OneSwiss.V8.Edt;

[MessagePackObject]
public class EdtInstallation
{
    [Key(0)]
    public string Version { get; set; }
    [Key(1)]
    public string Path { get; set; }
    [Key(2)]
    public bool HasEdtCli { get; set; }
    [Key(3)]
    public bool FromStarter { get; set; }
}