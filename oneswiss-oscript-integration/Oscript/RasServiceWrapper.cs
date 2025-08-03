using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OneScript.Contexts;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("СервисRas", "RasService")]
public class RasServiceWrapper(RasService service) : AutoContext<RasServiceWrapper>
{
    [ContextProperty("Имя", "Name", CanWrite = false)]
    public string Name => service.Name;
    
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    public bool IsActive => service.IsActive;
    
    [ContextProperty("ХостRagent", "RagentHost", CanWrite = false)]
    public string RagentHost => service.RagentHost;

    [ContextProperty("ПортRagent", "RagentPort", CanWrite = false)]
    public int RagentPort => service.RagentPort;

    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port => service.Port;

    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8PlatformWrapper Platform { get; } = new V8PlatformWrapper(service.Platform);
}