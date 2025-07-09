using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Agent.Services;

public class V8ServicesProvider(V8PlatformsProvider v8PlatformsProvider)
{
    public List<RasService> GetRasServices()
        => V8Services.GetRasServices(v8PlatformsProvider.GetInstalledPlatforms());
    
    public RagentService GetActiveRagentForClusterPort(int port)
        => V8Services.GetActiveRagentForClusterPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    
    public List<RagentService> GetActiveRagentServices()
        => V8Services.GetActiveRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    public List<RagentService> GetRagentServices()
        => V8Services.GetRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    public CrServer GetCrServerForPort(int port)
        => V8Services.GetCrServerForPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    
    public List<CrServer> GetActiveCrServerServices()
        => V8Services.GetActiveCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    public List<CrServer> GetCrServerServices()
        => V8Services.GetCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
}