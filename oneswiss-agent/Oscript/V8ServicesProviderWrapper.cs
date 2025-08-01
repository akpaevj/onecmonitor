using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.OneScript.Oscript;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерСлужб", "ServicesProvider")]
public class V8ServicesProviderWrapper(V8ServicesProvider provider) : AutoContext<V8ServicesProviderWrapper>
{
    [ContextMethod("ПолучитьСлужбыRas", "GetRasServices")]
    public ArrayImpl GetRasServices()
        => new(provider.GetRasServices().Select(c => new RasServiceWrapper(c)));
    
    [ContextMethod("ПолучитьЗапущеннуюСлужбуRagentДляПортаКластера", "GetActiveRagentForClusterPort")]
    public RagentServiceWrapper GetActiveRagentForClusterPort(int port)
        => new(provider.GetActiveRagentForClusterPort(port));
    
    [ContextMethod("ПолучитьЗапущенныеСлужбыRas", "GetActiveRagentServices")]
    public ArrayImpl GetActiveRagentServices()
        => new(provider.GetActiveRagentServices().Select(c => new RagentServiceWrapper(c)));
    
    [ContextMethod("ПолучитьСлужбыRagent", "GetRagentServices")]
    public ArrayImpl GetRagentServices()
        => new(provider.GetRagentServices().Select(c => new RagentServiceWrapper(c)));
    
    [ContextMethod("ПолучитьСлужбуCrServerДляПорта", "GetCrServerForPort")]
    public CrServerWrapper GetCrServerForPort(int port)
        => new(provider.GetCrServerForPort(port));
    
    [ContextMethod("ПолучитьЗапущенныеСлужбыCrServer", "GetActiveCrServerServices")]
    public ArrayImpl GetActiveCrServerServices()
        => new(provider.GetActiveCrServerServices().Select(c => new CrServerWrapper(c)));
    
    [ContextMethod("ПолучитьСлужбыCrServer", "GetCrServerServices")]
    public ArrayImpl GetCrServerServices()
        => new(provider.GetCrServerServices().Select(c => new CrServerWrapper(c)));
}