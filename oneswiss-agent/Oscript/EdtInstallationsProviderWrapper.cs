using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерИнсталляцийEdt", "EdtInstallationsProvider")]
public class EdtInstallationsProviderWrapper(EdtInstallationsProvider provider) : AutoContext<EdtInstallationsProviderWrapper>
{
    [ContextMethod("ПолучитьИнсталляции", "GetInstallations")]
    public ArrayImpl GetInstallations()
    {
        return [.. provider.GetInstallations().Cast<IValue>()];
    }
}
