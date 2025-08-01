using OneScript.Contexts;
using OneSwiss.V8.Edt;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("ИнсталляцияEDT", "EdtInstallation")]
public class EdtInstallationWrapper(EdtInstallation item) : AutoContext<EdtInstallationWrapper>
{
    [ContextProperty("Версия", "Version", CanWrite = false)]
    public string Version => item.Version;
    [ContextProperty("Путь", "Path", CanWrite = false)]
    public string Path => item.Path;
    [ContextProperty("СуществуетEdtCli", "ExistsEdtCli", CanWrite = false)]
    public bool HasEdtCli => item.HasEdtCli;
    [ContextProperty("ПутьEdtCli", "PathEdtCli", CanWrite = false)]
    public string EdtCliPath => item.EdtCliPath;
    [ContextProperty("ИзСтартера", "FromStarter", CanWrite = false)]
    public bool FromStarter => item.FromStarter;
}