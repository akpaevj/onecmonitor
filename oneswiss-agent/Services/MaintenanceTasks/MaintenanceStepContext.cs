using OneScript.Contexts;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.OneScript.Oscript;
using OneSwiss.V8.Designer.Agent;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Services.MaintenanceTasks;

public class MaintenanceStepContext
{
    public required MaintenanceTaskDto Task { get; init; }

    public InfoBaseDto? InfoBase { get; init; }

    public MaintenanceStepDto Step { get; set; } = null!;
    
    public string AccessCode { get; set; } = string.Empty;

    public Dictionary<Guid, string> Files { get; set; } = [];

    public List<MaintenanceTaskLogItemDto> Log { get; init; } = [];
    public Rac Rac { get; set; } = null!;

    public V8Platform Platform { get; set; } = null!;

    public bool UseDesignerAgent { get; init; }

    public DesignerAgentClient? DesignerAgentClient { get; set; }

    public OnecV8BatchMode GetBatchDesigner()
    {
        return OnecV8BatchMode.CreateDesignerBatch(Platform, $"{InfoBase!.Cluster.Host}:{InfoBase.Cluster.Port}",
            InfoBase.InfoBaseName);
    }

    public async Task<OnecV8BatchMode> StartDesignerAgent(string baseDirectoryPath)
    {
        var batch = OnecV8BatchMode.CreateDesignerBatch(Platform, $"{InfoBase!.Cluster.Host}:{InfoBase.Cluster.Port}",
            InfoBase.InfoBaseName);
        await batch.StartSshAgent(baseDirectoryPath);

        return batch;
    }

    public OnecV8BatchMode GetBatchEnterprise()
    {
        return OnecV8BatchMode.CreateEnterpriseBatch(Platform, $"{InfoBase!.Cluster.Host}:{InfoBase.Cluster.Port}",
            InfoBase.InfoBaseName);
    }
}