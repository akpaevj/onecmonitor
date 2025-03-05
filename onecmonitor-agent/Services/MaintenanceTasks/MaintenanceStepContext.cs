using System.Collections.Concurrent;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OneSTools.Common.Designer.Batch;
using OneSTools.Common.Platform;
using OneSTools.Common.Platform.RemoteAdministration;

namespace OnecMonitor.Agent.Services.MaintenanceTasks;

public class MaintenanceStepContext
{
    public InfoBaseDto InfoBase { get; set; } = null!;
    public MaintenanceStepDto Step { get; set; } = null!;
    public string AccessCode { get; set; } = string.Empty;
    public ConcurrentDictionary<Guid, string> V8Files { get; set; } = [];
    public List<MaintenanceStepLogItemDto> Log { get; set; } = [];
    public Rac Rac { get; set; } = null!;
    public V8Platform Platform { get; set; } = null!;
    
    public OnecV8BatchMode GetBatchDesigner() 
        => new(Platform, $"{InfoBase.Cluster.Host}:{InfoBase.Cluster.Port}", InfoBase.InfoBaseName);
                            
    public OnecV8BatchMode GetBatchEnterprise() 
        => new(Platform, $"{InfoBase.Cluster.Host}:{InfoBase.Cluster.Port}", InfoBase.InfoBaseName, false);
}