using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.V8.Platform.Services;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("МенеджерRas", "RasHolder")]
public class RasHolderWrapper(RasHolder holder) : AutoContext<RasHolderWrapper>
{
    [ContextMethod("ПолучитьСлужбыRas", "GetRasServices")]
    public ArrayImpl GetRasServices()
    {
        return [.. holder.GetRasServices().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьАктивныйRasДляRagent", "GetActiveRasForRagent")]
    public RasService GetActiveRasForRagent(RagentService ragent)
    {
        return holder.GetActiveRasForRagent(ragent);
    }
}
