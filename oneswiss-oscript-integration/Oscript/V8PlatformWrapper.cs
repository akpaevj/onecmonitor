using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OneScript.Contexts;
using OneSwiss.V8.Platform;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("ПлатформаV8", "V8Platform")]
public class V8PlatformWrapper(V8Platform platform) : AutoContext<V8PlatformWrapper>
{
    [ContextProperty("Путь", "Path", CanWrite = false)]
    public string PlatformPath => platform.PlatformPath;
    [ContextProperty("Существует1CV8", "Exists1CV8", CanWrite = false)]
    public bool HasOnecV8 => platform.HasOnecV8;
    [ContextProperty("Путь1CV8", "Path1CV8", CanWrite = false)]
    public string OnecV8Path => platform.OnecV8Path;
    [ContextProperty("СуществуетRac", "ExistsRac", CanWrite = false)]
    public bool HasRac => platform.HasRac;
    [ContextProperty("ПутьRac", "PathRac", CanWrite = false)]
    public string RacPath => platform.RacPath;
    [ContextProperty("СуществуетRas", "ExistsRas", CanWrite = false)]
    public bool HasRas => platform.HasRas;
    [ContextProperty("ПутьRas", "PathRas", CanWrite = false)]
    public string RasPath => platform.RasPath;
    [ContextProperty("СуществуетIbcmd", "ExistsIbcmd", CanWrite = false)]
    public bool HasIbcmd => platform.HasIbcmd;
    [ContextProperty("ПутьIbcmd", "PathIbcmd", CanWrite = false)]
    public string IbcmdPath => platform.IbcmdPath;
    [ContextProperty("Версия", "Version", CanWrite = false)]
    public string Version => platform.Version;
}