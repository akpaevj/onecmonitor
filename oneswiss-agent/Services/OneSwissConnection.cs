using OneSwiss.Agent.Models;
using OneSwiss.Common;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.V8.Edt;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Agent.Services;

public class OneSwissConnection(
    AgentInstance agent,
    TokenRetriever tokenRetriever,
    IConfiguration configuration,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<OneSwissConnection> logger)
    : ServerConnection(logger)
{
    public const string CommonKey = "Common";
    
    private readonly bool _authRequired = configuration.GetValue("Auth:Required", false);
    private readonly string _serverAddress = configuration.GetValue("Server", "ws://localhost:7002");

    public async Task Start(bool mainConnection = false)
    {
        await Start(_serverAddress, async () => _authRequired ? await tokenRetriever.GetValidTokenAsync() : null, async () =>
        {
            await WriteMessageToStream(MessageType.AgentInfo,
                new AgentInstanceDto
                {
                    Id = agent.Id,
                    InstanceName = agent.InstanceName,
                    MainConnection = mainConnection,
                    UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds
                },
                hostApplicationLifetime.ApplicationStopping);
        }, hostApplicationLifetime.ApplicationStopping);
    }

    public async Task<V8Platform> GetCrServerPlatform(Guid agentId, int port, CancellationToken cancellationToken)
    {
        return await Get<CrServerPlatformRequestDto, V8Platform>(
            MessageType.CrServerPlatformRequest,
            MessageType.V8Platform,
            new CrServerPlatformRequestDto
            {
                AgentId = agentId,
                Port = port
            }, cancellationToken);
    }

    public async Task Ok(Message request, CancellationToken cancellationToken)
    {
        await SendOk(request, cancellationToken);
    }

    public async Task Error(Message request, string message, CancellationToken cancellationToken)
    {
        await SendError(request, message, cancellationToken);
    }

    public async Task SendSystemInfo(Message request, SystemInfoDto item, CancellationToken cancellationToken)
    {
        await Send(MessageType.SystemInfo, item, request, cancellationToken);
    }

    public async Task SendConfigRepositoryDetails(Message request, ConfigRepositoryDetailsDto item,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.ConfigRepositoryDetails, item, request, cancellationToken);
    }

    public async Task SendInstalledPlatforms(Message request, List<V8Platform> items,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.InstalledPlatforms, items, request, cancellationToken);
    }

    public async Task SendEdtInstallations(Message request, List<EdtInstallation> items,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.EdtInstallations, items, request, cancellationToken);
    }

    public async Task SendCrServerServices(Message request, List<CrServer> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.CrServerServices, items, request, cancellationToken);
    }

    public async Task SendRasServices(Message request, List<RasService> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.RasServices, items, request, cancellationToken);
    }

    public async Task SendRagentServices(Message request, List<RagentService> items,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.RagentServices, items, request, cancellationToken);
    }

    public async Task SendV8Processes(Message request, List<V8Process> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.V8Processes, items, request, cancellationToken);
    }

    public async Task SendV8Sessions(Message request, List<V8Session> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.V8Sessions, items, request, cancellationToken);
    }

    public async Task SendV8ClusterDetails(Message request, V8ClusterDetails item, CancellationToken cancellationToken)
    {
        await Send(MessageType.ClusterDetails, item, request, cancellationToken);
    }

    public async Task SendV8Clusters(Message request, List<V8Cluster> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.Clusters, items, request, cancellationToken);
    }

    public async Task SendV8InfoBaseDetails(Message request, V8InfoBaseDetails item,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.InfoBaseDetails, item, request, cancellationToken);
    }

    public async Task SendV8InfoBases(Message request, List<V8InfoBase> items, CancellationToken cancellationToken)
    {
        await Send(MessageType.InfoBases, items, request, cancellationToken);
    }

    public async Task SendMaintenanceStepNodeLog(List<MaintenanceTaskLogItemDto> items,
        CancellationToken cancellationToken)
    {
        await Send(MessageType.MaintenanceStepNodeLog, items, cancellationToken);
    }

    public async Task NotifyGitSyncTaskItemProcessorStopped(Guid taskId, Guid repoId, string reason,
        CancellationToken cancellationToken)
    {
        await Send(
            MessageType.GitSyncTaskProcessorStopped,
            new GitSyncTasksProcessorStopped
            {
                TaskId = taskId,
                ConfigurationRepositoryId = repoId,
                Reason = reason
            }, cancellationToken);
    }

    public async Task NotifyGitSyncTaskProcessorStopped(Guid taskId, string reason,
        CancellationToken cancellationToken)
    {
        await Send(
            MessageType.GitSyncTaskProcessorStopped,
            new GitSyncTasksProcessorStopped
            {
                TaskId = taskId,
                Reason = reason
            }, cancellationToken);
    }

    public async Task QueueCustomNotification(string key, string message,
        CancellationToken cancellationToken)
    {
        await Send(
            MessageType.QueueCustomNotificationRequest,
            new CustomNotificationDto
            {
                Key = key,
                Message = message
            }, cancellationToken);
    }

    public async Task DownloadFile(Stream stream, Guid fileId, CancellationToken cancellationToken)
    {
        await ToStream(
            stream,
            MessageType.FileRequest,
            new FileRequestDto
            {
                Id = fileId
            }, cancellationToken);
    }

    public async Task<List<GitSyncTaskDto>> GetGitSyncTasks(CancellationToken cancellationToken)
    {
        return await Get<List<GitSyncTaskDto>>(
            MessageType.GitSyncTasksRequest,
            MessageType.GitSyncTasks,
            cancellationToken);
    }

    public async Task<SettingsDto> GetSettings(CancellationToken cancellationToken)
    {
        return await Get<SettingsDto>(
            MessageType.SettingsRequest,
            MessageType.Settings,
            cancellationToken);
    }
}