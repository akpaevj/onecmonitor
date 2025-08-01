using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.OneScript.Oscript;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерИнсталляцийEDT", "EdtInstallationsProvider")]
public class EdtInstallationsProviderWrapper(EdtInstallationsProvider provider) : AutoContext<EdtInstallationsProviderWrapper>
{
    [ContextMethod("ПолучитьИнсталляции", "GetInstallations")]
    public ArrayImpl GetInstallations()
        => new(provider.GetInstallations().Select(c => new EdtInstallationWrapper(c)));
}