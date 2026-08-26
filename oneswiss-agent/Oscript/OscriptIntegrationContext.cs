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
    public V8PlatformsProviderWrapper PlatformsProvider { get; } = new(platformsProvider);

    [ContextProperty("ПровайдерСлужб", "ServicesProvider", CanWrite = false)]
    public V8ServicesProviderWrapper ServicesProvider { get; } = new(servicesProvider);

    [ContextProperty("МенеджерRas", "RasManager", CanWrite = false)]
    public RasHolderWrapper RasHolder { get; } = new(rasHolder);

    [ContextProperty("ПровайдерИнсталляцийEdt", "EdtInstallationsProvider", CanWrite = false)]
    public EdtInstallationsProviderWrapper EdtInstallationsProvider { get; } = new(edtInstallationsProvider);

    [ContextProperty("Сервер", "Server", CanWrite = false)]
    public OneSwissConnectionWrapper Server { get; } = new(serverConnection);
}