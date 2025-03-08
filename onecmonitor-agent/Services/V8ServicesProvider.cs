using OneSTools.Common.Platform.Services;

namespace OnecMonitor.Agent.Services;

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
}