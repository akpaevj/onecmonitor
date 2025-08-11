using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Agent.Services;

[ContextClass("ПровайдерСлужб", "ServicesProvider")]
public class V8ServicesProvider(V8PlatformsProvider v8PlatformsProvider)
{
    [ContextMethod("ПолучитьСлужбыRas", "GetRasServices", Converter = typeof(ListContextConverter<RasService>))]
    public List<RasService> GetRasServices()
        => V8Services.GetRasServices(v8PlatformsProvider.GetInstalledPlatforms());
    
    [ContextMethod("ПолучитьЗапущеннуюСлужбуRagentДляПортаКластера", "GetActiveRagentForClusterPort")]
    public RagentService GetActiveRagentByPort(int port)
        => V8Services.GetActiveRagentByPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    
    [ContextMethod("ПолучитьЗапущенныеСлужбыRagent", "GetActiveRagentServices", Converter = typeof(ListContextConverter<RagentService>))]
    public List<RagentService> GetActiveRagentServices()
        => V8Services.GetActiveRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    [ContextMethod("ПолучитьСлужбыRagent", "GetRagentServices", Converter = typeof(ListContextConverter<RagentService>))]
    public List<RagentService> GetRagentServices()
        => V8Services.GetRagentServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    [ContextMethod("ПолучитьСлужбуCrServerДляПорта", "GetCrServerForPort")]
    public CrServer GetCrServerForPort(int port)
        => V8Services.GetCrServerForPort(port, v8PlatformsProvider.GetInstalledPlatforms());
    
    [ContextMethod("ПолучитьЗапущенныеСлужбыCrServer", "GetActiveCrServerServices", Converter = typeof(ListContextConverter<CrServer>))]
    public List<CrServer> GetActiveCrServerServices()
        => V8Services.GetActiveCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
    
    [ContextMethod("ПолучитьСлужбыCrServer", "GetCrServerServices", Converter = typeof(ListContextConverter<CrServer>))]
    public List<CrServer> GetCrServerServices()
        => V8Services.GetCrServerServices(v8PlatformsProvider.GetInstalledPlatforms()).ToList();
}