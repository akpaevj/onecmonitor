using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using OneSwiss.OneScript.Oscript;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерПлатформV8", "V8PlatformsProvider")]
public class V8PlatformsProviderWrapper(V8PlatformsProvider provider) : AutoContext<V8PlatformsProviderWrapper>
{
    [ContextMethod("ПолучитьУстановленныеПлатформы", "GetInstalledPlatforms")]
    public ArrayImpl GetInstalledPlatforms()
        => new (provider.GetInstalledPlatforms().Select(c => new V8PlatformWrapper(c)));
}