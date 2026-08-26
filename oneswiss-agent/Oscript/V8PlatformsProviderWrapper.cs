using OneScript.Contexts;
using OneScript.StandardLibrary.Collections;
using OneSwiss.Agent.Services;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Agent.Oscript;

[ContextClass("ПровайдерПлатформV8", "V8PlatformsProvider")]
public class V8PlatformsProviderWrapper(V8PlatformsProvider provider) : AutoContext<V8PlatformsProviderWrapper>
{
    [ContextMethod("ПолучитьУстановленныеПлатформы", "GetInstalledPlatforms")]
    public ArrayImpl GetInstalledPlatforms()
    {
        return [.. provider.GetInstalledPlatforms().Cast<IValue>()];
    }

    [ContextMethod("ПолучитьКаталогиУстановкиПлатформ", "GetExistsPlatformInstallationPaths")]
    public ArrayImpl GetExistsPlatformInstallationPaths()
    {
        return [.. provider.GetExistsPlatformInstallationPaths().Select(c => (IValue)ValueFactory.Create(c))];
    }
}
