using OneScript.Contexts;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("СервисRagent", "RagentService")]
public class RagentServiceWrapper(RagentService service) : AutoContext<RagentServiceWrapper>
{
    public readonly RagentService Service = service;
    
    [ContextProperty("Имя", "Name", CanWrite = false)]
    public string Name => Service.Name;
    
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    public bool IsActive => Service.IsActive;
    
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port => Service.Port;

    [ContextProperty("ПортReg", "RegPort", CanWrite = false)]
    public int RegPort => Service.RegPort;
    
    [ContextProperty("КаталогКластера", "ClusterCatalog", CanWrite = false)]
    public string ClusterCatalog => Service.ClusterCatalog;
    
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8PlatformWrapper Platform { get; } = new(service.Platform);

    [ContextProperty("ТипОтладки", "DebugType", CanWrite = false)]
    public RagentDebugTypeWrapper DebugType { get; } = (RagentDebugTypeWrapper)service.DebugType;
}