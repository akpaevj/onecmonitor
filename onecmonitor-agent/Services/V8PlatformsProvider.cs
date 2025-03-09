using OneSTools.Common.Platform;

namespace OnecMonitor.Agent.Services;

public class V8PlatformsProvider(IConfiguration configuration)
{
    private readonly string[] _additionalPaths = configuration.GetSection("V8:PlatformPaths").Get<string[]>() ?? [];
    
    public IReadOnlyList<V8Platform> GetInstalledPlatforms()
        => V8Platforms.GetInstalledPlatforms(_additionalPaths);
}