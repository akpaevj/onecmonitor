using System.Net.WebSockets;
using AutoMapper;
using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.V8.Edt;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Server.Services;

public class AgentConnection : FastConnection
{
    public delegate void AgentConnectedHandler(AgentConnection agentConnection);

    public delegate void AgentDisconnectedHandler(AgentConnection agentConnection);

    private readonly InterAgencyCommunicationService _interAgencyCommunicationService;
    private readonly ILogger<AgentConnection> _logger;
    private readonly IMapper _mapper;
    private readonly NotificationsService _notificationsService;
    private readonly IServiceProvider _serviceProvider;

    public AgentConnection(
        WebSocket socket,
        IServiceProvider serviceProvider,
        InterAgencyCommunicationService interAgencyCommunicationService,
        ILogger<AgentConnection> logger)
        : base(logger)
    {
        Socket = socket;

        Disconnected += (_, _) => { AgentDisconnected?.Invoke(this); };

        ConnectionId = Guid.NewGuid();
        _mapper = serviceProvider.GetRequiredService<IMapper>();
        _serviceProvider = serviceProvider;
        _notificationsService = serviceProvider.GetRequiredService<NotificationsService>();
        _logger = logger;
        _interAgencyCommunicationService = interAgencyCommunicationService;
    }

    public Guid ConnectionId { get; }
    public AgentInstanceDto? AgentInstance { get; private set; }
    public event AgentConnectedHandler? AgentConnected;
    public event AgentDisconnectedHandler? AgentDisconnected;

    public void Listen(CancellationToken cancellationToken = default)
    {
        MessageReceived += async (_, message) =>
        {
            try
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (message.Header.Type)
                {
                    case MessageType.AgentInfo:
                        await HandleInitMessage(message, cancellationToken);
                        break;
                    case MessageType.MaintenanceStepNodeLog:
                        await HandleMaintenanceStepLog(message, cancellationToken);
                        break;
                    case MessageType.SettingsRequest:
                        await HandleSettingsRequest(message, cancellationToken);
                        break;
                    case MessageType.GitSyncTasksRequest:
                        await HandleGitSyncTasksRequest(message, cancellationToken);
                        break;
                    case MessageType.FileRequest:
                        await SendFile(message, cancellationToken);
                        break;
                    case MessageType.QueueCustomNotificationRequest:
                        await QueueCustomNotification(message, cancellationToken);
                        break;
                    case MessageType.GitSyncTaskProcessorStopped:
                        await HandleGitSyncTaskProcessorStopped(message, cancellationToken);
                        break;
                    case MessageType.CrServerPlatformRequest:
                        await HandleCrServerPlatformRequest(message, cancellationToken);
                        break;
                    default:
                        throw new Exception($"Получено неожиданное сообщение: {message.Header.Type}");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Ошибка обработки сообщения");
                await SendError(message, ex.ToString(), cancellationToken);
            }
        };

        RunStreamLoops(cancellationToken);
    }

    private async Task HandleCrServerPlatformRequest(Message message, CancellationToken cancellationToken)
    {
        var request = ParseMessageData<CrServerPlatformRequestDto>(message.Data, cancellationToken);
        var service = _interAgencyCommunicationService.GetCrServerPlatform(request, cancellationToken);

        await Send(MessageType.V8Platform, service, message, cancellationToken);
    }

    private async Task<List<GitSyncTaskDto>> GetGitSyncTasks(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tasks = await dbContext.GitSyncTasks
            .AsNoTracking()
            .Include(c => c.Items).ThenInclude(c => c.ConfigurationRepository.Agent)
            .Include(c => c.Items).ThenInclude(c => c.ConfigurationRepository.Credentials)
            .Include(c => c.Items).ThenInclude(c => c.ConfigurationRepository.Users)
            .Include(c => c.GitRepository.Token)
            .Where(c => c.AgentId == AgentInstance!.Id)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<GitSyncTaskDto>>(tasks);
    }

    private async Task<SettingsDto> GetSettings(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventLogSettings =
            await dbContext.EventLogSettings
                .AsNoTracking()
                .Include(c => c.Dbms)
                .Include(c => c.Credentials)
                .SingleOrDefaultAsync(cancellationToken) ?? new EventLogSettings();

        var techLogSettings =
            await dbContext.TechLogSettings
                .AsNoTracking()
                .Include(c => c.Dbms)
                .Include(c => c.Credentials)
                .SingleOrDefaultAsync(cancellationToken) ?? new TechLogSettings();

        var gitSyncSettings =
            await dbContext.GitSyncSettings
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken) ?? new GitSyncSettings();

        var techLogSettingsDto = _mapper.Map<TechLogSettingsDto>(techLogSettings);
        techLogSettingsDto.Seances = await GetTechLogSeances(cancellationToken);

        var settings = new SettingsDto
        {
            EventLogSettings = _mapper.Map<EventLogSettingsDto>(eventLogSettings),
            TechLogSettings = techLogSettingsDto,
            GitSyncSettings = _mapper.Map<GitSyncSettingsDto>(gitSyncSettings)
        };

        return settings;
    }

    private async Task HandleSettingsRequest(Message message, CancellationToken cancellationToken)
    {
        await Send(MessageType.Settings, await GetSettings(cancellationToken), message, cancellationToken);
    }

    private async Task HandleGitSyncTasksRequest(Message message, CancellationToken cancellationToken)
    {
        await Send(MessageType.GitSyncTasks, await GetGitSyncTasks(cancellationToken), message, cancellationToken);
    }

    private async Task HandleGitSyncTaskProcessorStopped(Message message, CancellationToken cancellationToken)
    {
        var request = ParseMessageData<GitSyncTasksProcessorStopped>(message.Data, cancellationToken);

        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.GitSyncTasks
            .Include(gitSyncTask => gitSyncTask.Items)
            .FirstOrDefaultAsync(c => c.Id == request.TaskId, cancellationToken);

        if (request.ConfigurationRepositoryId == Guid.Empty)
        {
            task!.IsActive = false;
        }
        else
        {
            var taskItem =
                task!.Items.FirstOrDefault(c => c.ConfigurationRepositoryId == request.ConfigurationRepositoryId);
            taskItem!.IsActive = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await _notificationsService.QueueGitSyncStopped(task!.Id, request.ConfigurationRepositoryId, request.Reason,
            cancellationToken);

        await SendOk(message, cancellationToken);
    }

    public async Task SendSettingsRequest(CancellationToken cancellationToken)
    {
        await Send(MessageType.Settings, await GetSettings(cancellationToken), cancellationToken);
    }

    public async Task SendGitSyncTasks(CancellationToken cancellationToken)
    {
        await Send(MessageType.GitSyncTasks, await GetGitSyncTasks(cancellationToken), cancellationToken);
    }

    public async Task<List<V8Platform>> GetInstalledPlatforms(CancellationToken cancellationToken)
    {
        return await Get<List<V8Platform>>(
            MessageType.InstalledPlatformsRequest,
            MessageType.InstalledPlatforms,
            cancellationToken);
    }

    public async Task<List<V8Cluster>> GetV8Clusters(CancellationToken cancellationToken)
    {
        return await Get<List<V8Cluster>>(
            MessageType.ClustersRequest,
            MessageType.Clusters,
            cancellationToken);
    }

    public async Task<V8ClusterDetails> GetV8ClusterDetails(Cluster cluster, CancellationToken cancellationToken)
    {
        return await Get<ClusterDto, V8ClusterDetails>(
            MessageType.ClusterDetailsRequest,
            MessageType.ClusterDetails,
            _mapper.Map<ClusterDto>(cluster),
            cancellationToken);
    }

    public async Task ChangeClusterParameters(Cluster cluster, Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        await Send(
            MessageType.ChangeClusterParametersRequest,
            new ChangeClusterObjectParametersRequestDto<ClusterDto>
            {
                Item = _mapper.Map<ClusterDto>(cluster),
                Parameters = parameters
            },
            cancellationToken);
    }

    public async Task<List<RagentService>> GetRagentServices(CancellationToken cancellationToken)
    {
        return await Get<List<RagentService>>(
            MessageType.RagentServicesRequest,
            MessageType.RagentServices,
            cancellationToken);
    }

    public async Task<List<CrServer>> GetCrServerServices(CancellationToken cancellationToken)
    {
        return await Get<List<CrServer>>(
            MessageType.CrServerServicesRequest,
            MessageType.CrServerServices,
            cancellationToken);
    }

    public async Task<List<RasService>> GetRasServices(CancellationToken cancellationToken)
    {
        return await Get<List<RasService>>(
            MessageType.RasServicesRequest,
            MessageType.RasServices,
            cancellationToken);
    }

    public async Task<SystemInfoDto> GetSystemInfo(CancellationToken cancellationToken)
    {
        return await Get<SystemInfoDto>(
            MessageType.SystemInfoRequest,
            MessageType.SystemInfo,
            cancellationToken);
    }

    public async Task<List<EdtInstallation>> GetEdtInstallations(CancellationToken cancellationToken)
    {
        return await Get<List<EdtInstallation>>(
            MessageType.EdtInstallationsRequest,
            MessageType.EdtInstallations,
            cancellationToken);
    }

    public async Task<List<V8InfoBase>> GetV8InfoBases(Cluster cluster, CancellationToken cancellationToken)
    {
        return await Get<ClusterDto, List<V8InfoBase>>(
            MessageType.InfoBasesRequest,
            MessageType.InfoBases,
            _mapper.Map<ClusterDto>(cluster),
            cancellationToken);
    }

    public async Task<V8InfoBaseDetails> GetV8InfoBaseDetails(InfoBase infoBase, CancellationToken cancellationToken)
    {
        return await Get<InfoBaseDto, V8InfoBaseDetails>(
            MessageType.InfoBaseDetailsRequest,
            MessageType.InfoBaseDetails,
            _mapper.Map<InfoBaseDto>(infoBase),
            cancellationToken);
    }

    public async Task ChangeInfoBaseParameters(InfoBase item, Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        await Send(
            MessageType.ChangeInfoBaseParametersRequest,
            new ChangeClusterObjectParametersRequestDto<InfoBaseDto>
            {
                Item = _mapper.Map<InfoBaseDto>(item),
                Parameters = parameters
            },
            cancellationToken);
    }

    public async Task<List<V8Session>> GetV8Sessions(Cluster cluster, InfoBase? infoBase,
        CancellationToken cancellationToken)
    {
        return await Get<V8SessionsRequestDto, List<V8Session>>(
            MessageType.V8SessionsRequest,
            MessageType.V8Sessions,
            new V8SessionsRequestDto
            {
                Cluster = _mapper.Map<ClusterDto>(cluster),
                InfoBase = infoBase == null ? null : _mapper.Map<InfoBaseDto>(infoBase)
            },
            cancellationToken);
    }

    public async Task<List<V8Process>> GetV8Processes(Cluster cluster, CancellationToken cancellationToken)
    {
        return await Get<ClusterDto, List<V8Process>>(
            MessageType.V8ProcessesRequest,
            MessageType.V8Processes,
            _mapper.Map<ClusterDto>(cluster),
            cancellationToken);
    }

    public async Task CloseV8Sessions(Cluster cluster, List<string> sessionsIds, CancellationToken cancellationToken)
    {
        await Send(
            MessageType.CloseV8SessionsRequest,
            new CloseV8SessionsRequestDto
            {
                Cluster = _mapper.Map<ClusterDto>(cluster),
                SessionsIds = sessionsIds
            },
            cancellationToken);
    }

    public async Task<ConfigRepositoryDetailsDto> GetConfigRepositoryDetails(int port, string repository,
        CancellationToken cancellationToken)
    {
        return await Get<ConfigRepositoryDetailsRequestDto, ConfigRepositoryDetailsDto>(
            MessageType.ConfigRepositoryDetailsRequest,
            MessageType.ConfigRepositoryDetails,
            new ConfigRepositoryDetailsRequestDto
            {
                CrServerPort = port,
                Repository = repository
            },
            cancellationToken);
    }

    public async Task StartMaintenanceTask(MaintenanceTaskDto task, CancellationToken cancellationToken = default)
    {
        await Send(MessageType.MaintenanceTask, _mapper.Map<MaintenanceTaskDto>(task), cancellationToken);
    }

    private async Task<List<TechLogSeanceDto>> GetTechLogSeances(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agent = await dbContext.Agents
            .AsNoTracking()
            .Include(c => c.TechLogSeances).ThenInclude(c => c.Templates)
            .FirstOrDefaultAsync(c => c.Id == AgentInstance!.Id, cancellationToken);

        var seances = new List<TechLogSeanceDto>();

        agent!.TechLogSeances.ForEach(seance =>
        {
            seance.Templates.ForEach(template =>
            {
                seances.Add(new TechLogSeanceDto
                {
                    Id = seance.Id,
                    StartDateTime = seance.StartDateTime,
                    FinishDateTime = seance.FinishDateTime,
                    TemplateId = template.Id,
                    Template = template.Content
                });
            });
        });

        return seances;
    }

    private async Task HandleInitMessage(Message message, CancellationToken cancellationToken)
    {
        AgentInstance = ParseMessageData<AgentInstanceDto>(message.Data, cancellationToken);

        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var foundItem = await dbContext.Agents.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == AgentInstance.Id, cancellationToken);

            if (foundItem == null)
            {
                var agent = new Agent
                {
                    Id = AgentInstance.Id,
                    InstanceName = AgentInstance.InstanceName
                };
                dbContext.Agents.Add(agent);

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (foundItem.InstanceName != AgentInstance.InstanceName)
            {
                foundItem.InstanceName = AgentInstance.InstanceName;

                dbContext.Entry(foundItem).State = EntityState.Modified;

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await dbContext.Database.CommitTransactionAsync(cancellationToken);

            AgentConnected?.Invoke(this);
        }
        catch (Exception ex)
        {
            await dbContext.Database.RollbackTransactionAsync(cancellationToken);
        }
    }

    private async Task HandleMaintenanceStepLog(Message requestMessage, CancellationToken cancellationToken)
    {
        var result = ParseMessageData<List<MaintenanceTaskLogItemDto>>(requestMessage.Data, cancellationToken);

        var log = _mapper.Map<List<MaintenanceTaskLogItem>>(result);

        if (log.Count > 0)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var taskLogHub = scope.ServiceProvider.GetRequiredService<IHubContext<MaintenanceTaskLogHub>>();

            await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                log.ForEach(c => c.TimeStamp = c.TimeStamp.AddSeconds(AgentInstance!.UtcOffset));
                await dbContext.MaintenanceTaskLogs.AddRangeAsync(log, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);

                var task = await dbContext.MaintenanceTasks
                    .Include(c => c.InfoBases)
                    .FirstOrDefaultAsync(c => c.Id == result[0].TaskId, cancellationToken);

                task!.IsFaulted = await dbContext.MaintenanceTaskLogs
                    .AsNoTracking()
                    .AnyAsync(c => c.TaskId == task.Id && c.IsError, cancellationToken);

                var hasTaskFinishLogItem = await dbContext.MaintenanceTaskLogs
                    .AsNoTracking()
                    .AnyAsync(c => c.TaskId == task.Id && c.StepId == null && c.IsFinish, cancellationToken);

                if (hasTaskFinishLogItem)
                    task.FinishDateTime = DateTime.Now;

                await dbContext.SaveChangesAsync(cancellationToken);
                await dbContext.Database.CommitTransactionAsync(cancellationToken);

                await SendOk(requestMessage, cancellationToken);

                if (hasTaskFinishLogItem)
                    await _notificationsService.QueueMaintenanceTaskCompleted(task.Id, cancellationToken);

                await taskLogHub.Clients.Group(task.Id.ToString()).SendAsync("LogUpdated", cancellationToken);
            }
            catch (Exception e)
            {
                await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(e, "Ошибка записи лога шага обслуживания");

                await SendError(requestMessage, e.Message, cancellationToken);
            }
        }
        else
        {
            await SendOk(requestMessage, cancellationToken);
        }
    }

    private async Task SendFile(Message message, CancellationToken cancellationToken)
    {
        var request = ParseMessageData<FileRequestDto>(message.Data, cancellationToken);

        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var filesProvider = scope.ServiceProvider.GetRequiredService<FilesProvider>();

        var file = await dbContext.Files.FindAsync([request.Id], cancellationToken);

        await using var stream = filesProvider.OpenDataStream(file!.DataPath, FileMode.Open);
        var sent = 0L;
        var buffer = new byte[100 * 1024 * 1024];

        await Send(MessageType.DataStreamHeader, new StreamDescription
        {
            Length = stream.Length
        }, message, cancellationToken);

        while (sent < stream.Length)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            await Send(MessageType.DataStreamChunk, buffer[..read], message, cancellationToken);
            sent += read;
        }

        await SendOk(message, cancellationToken);
    }

    private async Task QueueCustomNotification(Message message, CancellationToken cancellationToken)
    {
        var notification = ParseMessageData<CustomNotificationDto>(message.Data, cancellationToken);
        await _notificationsService.QueueCustomNotification(notification.Key, notification.Message, cancellationToken);

        await SendOk(message, cancellationToken);
    }

    private static T ParseMessageData<T>(ReadOnlyMemory<byte> messageData, CancellationToken cancellationToken)
    {
        return MessagePackSerializer.Deserialize<T>(messageData, cancellationToken: cancellationToken);
    }

    public override bool Equals(object? obj)
    {
        return obj is AgentConnection connection &&
               ConnectionId.Equals(connection.ConnectionId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ConnectionId);
    }
}