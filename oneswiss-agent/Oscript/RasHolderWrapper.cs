using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.OneScript.Oscript;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("МенеджерRas", "RasManager")]
public class RasHolderWrapper(RasHolder holder) : AutoContext<RasHolderWrapper>
{
    [ContextMethod("ПолучитьСлужбыRas", "GetRasServices")]
    public ArrayImpl GetRasServices()
        => new (holder.GetRasServices().Select(c => new RasServiceWrapper(c)));
    
    [ContextMethod("ПолучитьЗапущеннуюСлужбуRasДляRagent", "GetActiveRasForRagent")]
    public RasServiceWrapper GetActiveRasForRagent(RagentServiceWrapper ragent)
        => new(holder.GetActiveRasForRagent(ragent.Service));
}