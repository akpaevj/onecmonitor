using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерСлужбV8", "V8ServicesProvider")]
public class V8ServicesProviderWrapper(V8ServicesProvider provider) : AutoContext<V8ServicesProviderWrapper>
{
    [ContextMethod("ПолучитьСлужбыRas", "GetRasServices")]
    public ArrayImpl GetRasServices()
    {
        return [.. provider.GetRasServices().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьАктивныйRagentПоПорту", "GetActiveRagentByPort")]
    public RagentService GetActiveRagentByPort(int port)
    {
        return provider.GetActiveRagentByPort(port);
    }

    [ContextMethod("ПолучитьАктивныеСлужбыRagent", "GetActiveRagentServices")]
    public ArrayImpl GetActiveRagentServices()
    {
        return [.. provider.GetActiveRagentServices().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьСлужбыRagent", "GetRagentServices")]
    public ArrayImpl GetRagentServices()
    {
        return [.. provider.GetRagentServices().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьСлужбуCrServerПоПорту", "GetCrServerForPort")]
    public CrServer GetCrServerForPort(int port)
    {
        return provider.GetCrServerForPort(port);
    }

    [ContextMethod("ПолучитьАктивныеСлужбыCrServer", "GetActiveCrServerServices")]
    public ArrayImpl GetActiveCrServerServices()
    {
        return [.. provider.GetActiveCrServerServices().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьСлужбыCrServer", "GetCrServerServices")]
    public ArrayImpl GetCrServerServices()
    {
        return [.. provider.GetCrServerServices().Cast<IValue>()];
    }
}
