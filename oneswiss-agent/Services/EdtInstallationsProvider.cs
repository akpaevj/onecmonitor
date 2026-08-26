using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Edt;

namespace OneSwiss.Agent.Services;

public class EdtInstallationsProvider(IConfiguration configuration)
{
    private readonly EdtPath[] _additionalPaths = configuration.GetSection("EDT:Paths").Get<EdtPath[]>() ?? [];

    public IReadOnlyList<EdtInstallation> GetInstallations()
    {
        return EdtDiscoverer.GetInstalled(_additionalPaths.Select(c => (c.IsStarter, c.Path)).ToArray());
    }

    private abstract record EdtPath(string Path, bool IsStarter);
}