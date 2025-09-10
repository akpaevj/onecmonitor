using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services;

[ContextClass("ПровайдерПлатформV8", "V8PlatformsProvider")]
public class V8PlatformsProvider(IConfiguration configuration)
{
    private readonly string[] _additionalPaths = configuration.GetSection("V8:PlatformPaths").Get<string[]>() ?? [];

    public string[] GetExistsPlatformInstallationPaths()
    {
        return V8Platforms.GetDefaultInstallationPaths()
            .Concat(_additionalPaths)
            .Where(Directory.Exists)
            .ToArray();
    }

    [ContextMethod("ПолучитьУстановленныеПлатформы", "GetInstalledPlatforms",
        Converter = typeof(ReadOnlyListContextConverter<V8Platform>))]
    public IReadOnlyList<V8Platform> GetInstalledPlatforms()
    {
        return V8Platforms.GetInstalledPlatforms(_additionalPaths);
    }
}