using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneScript.Values;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

[ContextClass("СлужбаCrServer", "CrServerService")]
public class CrServerWrapper(CrServer service) : AutoContext<CrServerWrapper>
{
    [ContextProperty("Имя", "Name", CanWrite = false)]
    public string Name => service.Name;
    
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    public bool IsActive => service.IsActive;
    
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }
    
    [ContextProperty("Каталог", "Directory", CanWrite = false)]
    public string Directory { get; set; } = null!;
    
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8PlatformWrapper Platform { get; } = new(service.Platform);

    [ContextProperty("Хранилища", "Repositories", CanWrite = false)]
    public ArrayImpl Repositories { get; } = new(service.Repositories.Select(BslStringValue.Create));
}