using OneScript.Contexts;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Services.MaintenanceTasks;

[ContextClass("КонтекстШагаОбслуживания", "MaintenanceStepContext")]
public class MaintenanceStepContext
{
    [ContextProperty("ЗадачаОбслуживания", "MaintenanceTask")]
    public MaintenanceTaskDto Task { get; set; } = null!;
    public InfoBaseDto InfoBase { get; set; } = null!;
    public MaintenanceStepDto Step { get; set; } = null!;
    public string AccessCode { get; set; } = string.Empty;
    public Dictionary<Guid, string> Files { get; set; } = [];
    public List<MaintenanceStepLogItemDto> Log { get; set; } = [];
    public Rac Rac { get; set; } = null!;
    public V8Platform Platform { get; set; } = null!;
    
    public OnecV8BatchMode GetBatchDesigner() 
        => new(Platform, $"{InfoBase.Cluster.Host}:{InfoBase.Cluster.Port}", InfoBase.InfoBaseName);
                            
    public OnecV8BatchMode GetBatchEnterprise() 
        => new(Platform, $"{InfoBase.Cluster.Host}:{InfoBase.Cluster.Port}", InfoBase.InfoBaseName, false);
}