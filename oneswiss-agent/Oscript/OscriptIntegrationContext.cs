using OneScript.Contexts;
using OneSwiss.Agent.Services;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("КонтекстИнтеграцииOneSwiss", "OneSwissIntegrationContext")]
public class OscriptIntegrationContext(
    V8PlatformsProvider platformsProvider, 
    V8ServicesProvider servicesProvider,
    RasHolder rasHolder,
    EdtInstallationsProvider edtInstallationsProvider, 
    OneSwissConnection serverConnection) : AutoContext<OscriptIntegrationContext>
{
    [ContextProperty("ПровайдерПлатформ", "PlatformsProvider", CanWrite = false)]
    public V8PlatformsProvider PlatformsProvider { get; } = platformsProvider;
    
    [ContextProperty("ПровайдерСлужб", "ServicesProvider", CanWrite = false)]
    public V8ServicesProvider ServicesProvider { get; } = servicesProvider;
    
    [ContextProperty("МенеджерRas", "RasManager", CanWrite = false)]
    public RasHolder RasHolder { get; } = rasHolder;
    
    [ContextProperty("ПровайдерИнсталляцийEdt", "EdtInstallationsProvider", CanWrite = false)]
    public EdtInstallationsProvider EdtInstallationsProvider { get; } = edtInstallationsProvider;
    
    [ContextProperty("Сервер", "Server", CanWrite = false)]
    public OneSwissConnectionWrapper Server { get; } = new(serverConnection);
}