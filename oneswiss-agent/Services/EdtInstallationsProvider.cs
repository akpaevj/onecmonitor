using OneSwiss.V8.Edt;

namespace OneSwiss.Agent.Services;

public class EdtInstallationsProvider(IConfiguration configuration)
{
    private readonly EdtPath[] _additionalPaths = configuration.GetSection("EDT:Paths").Get<EdtPath[]>() ?? [];
    
    public IReadOnlyList<EdtInstallation> GetInstallations()
        => EdtDiscoverer.GetInstalled(_additionalPaths.Select(c => (c.IsStarter, c.Path)).ToArray());

    private abstract record EdtPath(string Path, bool IsStarter);
}