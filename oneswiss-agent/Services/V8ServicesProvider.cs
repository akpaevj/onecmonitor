using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Agent.Services;

public class V8ServicesProvider(V8PlatformsProvider v8PlatformsProvider)
{
    public List<RasService> GetRasServices()
    {
        return V8Services.GetRasServices(v8PlatformsProvider.GetInstalledPlatforms());
    }

    public RagentService GetActiveRagentByPort(int port)
    {
        return V8Services.GetActiveRagentByPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    }

    public List<RagentService> GetActiveRagentServices()
    {
        return V8Services.GetActiveRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    }

    public List<RagentService> GetRagentServices()
    {
        return V8Services.GetRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    }

    public CrServer GetCrServerForPort(int port)
    {
        return V8Services.GetCrServerForPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    }

    public List<CrServer> GetActiveCrServerServices()
    {
        return V8Services.GetActiveCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    }

    public List<CrServer> GetCrServerServices()
    {
        return V8Services.GetCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    }
}