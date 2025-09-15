using System.Net;
using System.Net.Sockets;
using System.Reflection;
using MessagePack;
using OneSwiss.Agent.Services.GitSync;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Services;
using OneSwiss.V8.ConfigurationRepository;
using OneSwiss.V8.Platform.RemoteAdministration;
using V8Cluster = OneSwiss.V8.Platform.RemoteAdministration.V8Cluster;

namespace OneSwiss.Agent.Services;

internal class CommandsWatcher
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly EdtInstallationsProvider _edtPInstallationsProvider;
    private readonly EventLogRepositoryManager _eventLogRepositoryManager;
    private readonly GitSyncTasksManager _gitSyncTasksManager;
    private readonly MonitorQueue<List<GitSyncTaskDto>> _gitSyncTasksQueue;
    private readonly ILogger<CommandsWatcher> _logger;
    private readonly MonitorQueue<MaintenanceTaskDto> _maintenanceTasksQueue;
    private readonly ILogger<Rac> _racLogger;
    private readonly RasHolder _rasHolder;
    private readonly OneSwissConnection _server;
    private readonly TechLogRepositoryManager _techLogRepositoryManager;
    private readonly V8PlatformsProvider _v8PlatformsProvider;
    private readonly V8ServicesProvider _v8ServicesProvider;

    public CommandsWatcher(
        IServiceProvider serviceProvider,
        TechLogRepositoryManager techLogRepositoryManager,
        EventLogRepositoryManager eventLogRepositoryManager,
        MonitorQueue<MaintenanceTaskDto> maintenanceTasksQueue,
        RasHolder rasHolder,
        IHostApplicationLifetime appLifetime,
        V8PlatformsProvider v8PlatformsProvider,
        V8ServicesProvider v8ServicesProvider,
        EdtInstallationsProvider edtPInstallationsProvider,
        ILogger<CommandsWatcher> logger,
        ILogger<Rac> racLogger,
        MonitorQueue<List<GitSyncTaskDto>> gitSyncTasksQueue,
        GitSyncTasksManager gitSyncTasksManager)
    {
        var scope = serviceProvider.CreateAsyncScope();
        _racLogger = racLogger;
        _gitSyncTasksQueue = gitSyncTasksQueue;
        _gitSyncTasksManager = gitSyncTasksManager;
        _rasHolder = rasHolder;
        _server = scope.ServiceProvider.GetRequiredService<OneSwissConnection>();
        _techLogRepositoryManager = techLogRepositoryManager;
        _eventLogRepositoryManager = eventLogRepositoryManager;
        _v8PlatformsProvider = v8PlatformsProvider;
        _v8ServicesProvider = v8ServicesProvider;
        _edtPInstallationsProvider = edtPInstallationsProvider;
        _maintenanceTasksQueue = maintenanceTasksQueue;
        _applicationLifetime = appLifetime;
        _logger = logger;

        _server.MessageReceived += MessageReceived;

        _applicationLifetime.ApplicationStopping.Register(() => { _server.MessageReceived -= MessageReceived; });
    }

    private async void MessageReceived(object? sender, Message message)
    {
        try
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (message.Header.Type)
            {
                case MessageType.InstalledPlatformsRequest:
                    await SendInstalledPlatforms(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.ClustersRequest:
                    await SendV8Clusters(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.ClusterDetailsRequest:
                    await SendV8ClusterDetails(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.ChangeClusterParametersRequest:
                    await ChangeClusterParameters(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.InfoBasesRequest:
                    await SendV8InfoBases(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.InfoBaseDetailsRequest:
                    await SendV8InfoBaseDetails(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.ChangeInfoBaseParametersRequest:
                    await ChangeInfoBaseParameters(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.RagentServicesRequest:
                    await SendRagentServices(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.V8SessionsRequest:
                    await SendV8Sessions(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.V8ProcessesRequest:
                    await SendV8Processes(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.RasServicesRequest:
                    await SendRasServices(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.CrServerServicesRequest:
                    await SendCrServerServices(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.SystemInfoRequest:
                    await SendSystemInfo(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.Settings:
                    await HandleSettings(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.GitSyncTasks:
                    await HandleGitSyncTasks(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.MaintenanceTask:
                    await HandleMaintenanceTask(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.ConfigRepositoryDetailsRequest:
                    await SendConfigRepositoryDetails(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.EdtInstallationsRequest:
                    await SendEdtInstallations(message, _applicationLifetime.ApplicationStopping);
                    break;
                case MessageType.CloseV8SessionsRequest:
                    await CloseV8Sessions(message, _applicationLifetime.ApplicationStopping);
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
            await _server.Error(message, ex.Message, _applicationLifetime.ApplicationStopping);
        }
    }

    private async Task HandleMaintenanceTask(Message message, CancellationToken cancellationToken)
    {
        var task = MessagePackSerializer.Deserialize<MaintenanceTaskDto>(message.Data,
            cancellationToken: cancellationToken);
        await _maintenanceTasksQueue.QueueAsync(task, cancellationToken);

        await _server.Ok(message, cancellationToken);
    }

    public async Task Start(CancellationToken token)
    {
        await _server.Start(true);

        await UpdateSettings(token);
        await RequestGitSyncTasks(token);
    }

    private async Task SendInstalledPlatforms(Message message, CancellationToken cancellationToken)
    {
        var platforms = _v8PlatformsProvider.GetInstalledPlatforms();
        await _server.SendInstalledPlatforms(message, platforms.ToList(), cancellationToken);
    }

    private void ApplySettings(SettingsDto settingsDto, CancellationToken cancellationToken)
    {
        _techLogRepositoryManager.UpdateSettings(settingsDto.TechLogSettings);
        _eventLogRepositoryManager.UpdateSettings(settingsDto.EventLogSettings);
        _gitSyncTasksManager.UpdateSettings(settingsDto.GitSyncSettings);
    }

    private async Task HandleSettings(Message message, CancellationToken cancellationToken)
    {
        var settings =
            MessagePackSerializer.Deserialize<SettingsDto>(message.Data, cancellationToken: cancellationToken);
        ApplySettings(settings, cancellationToken);

        await _server.Ok(message, cancellationToken);
    }

    private async Task HandleGitSyncTasks(Message message, CancellationToken cancellationToken)
    {
        var tasks = MessagePackSerializer.Deserialize<List<GitSyncTaskDto>>(message.Data,
            cancellationToken: cancellationToken);
        await _gitSyncTasksQueue.QueueAsync(tasks, cancellationToken);

        await _server.Ok(message, cancellationToken);
    }

    private async Task UpdateSettings(CancellationToken cancellationToken)
    {
        var response = await _server.GetSettings(cancellationToken);
        ApplySettings(response, cancellationToken);
    }

    private async Task RequestGitSyncTasks(CancellationToken cancellationToken)
    {
        var response = await _server.GetGitSyncTasks(cancellationToken);
        await _gitSyncTasksQueue.QueueAsync(response, cancellationToken);
    }

    private async Task SendV8Clusters(Message message, CancellationToken cancellationToken)
    {
        var ragents = _v8ServicesProvider.GetActiveRagentServices();
        var clusters = new List<V8Cluster>();

        foreach (var ragentService in ragents)
        {
            var ras = _rasHolder.GetActiveRasForRagent(ragentService);
            var rac = Rac.GetRacForRasService(_racLogger, ras);

            var ragentClusters = await rac.GetClusters();
            ragentClusters.ForEach(c => c.RagentPort = ragentService.Port);

            clusters.AddRange(ragentClusters);
        }

        await _server.SendV8Clusters(message, clusters, cancellationToken);
    }

    private async Task SendV8ClusterDetails(Message message, CancellationToken cancellationToken)
    {
        var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        var item = await rac.GetCluster(
            cluster.ClusterInternalId);

        await _server.SendV8ClusterDetails(message, item, cancellationToken);
    }

    private async Task ChangeClusterParameters(Message message, CancellationToken cancellationToken)
    {
        var request =
            MessagePackSerializer.Deserialize<ChangeClusterObjectParametersRequestDto<ClusterDto>>(message.Data,
                cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(request.Item.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        await rac.UpdateClusterParameters(request.Item.ClusterInternalId, request.Parameters);

        await _server.Ok(message, cancellationToken);
    }

    private async Task SendV8InfoBases(Message message, CancellationToken cancellationToken)
    {
        var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        var infoBases = await rac.GetInfoBases(cluster.ClusterInternalId, cluster.Credentials?.User ?? "",
            cluster.Credentials?.Password ?? "");

        await _server.SendV8InfoBases(message, infoBases, cancellationToken);
    }

    private async Task SendV8InfoBaseDetails(Message message, CancellationToken cancellationToken)
    {
        var infoBase =
            MessagePackSerializer.Deserialize<InfoBaseDto>(message.Data, cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(infoBase.Cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        var item = await rac.GetInfoBase(
            infoBase.Cluster.ClusterInternalId,
            infoBase.InfoBaseInternalId,
            infoBase.Cluster.Credentials?.User ?? "",
            infoBase.Cluster.Credentials?.Password ?? "",
            infoBase.Credentials?.User ?? "",
            infoBase.Credentials?.Password ?? "");

        await _server.SendV8InfoBaseDetails(message, item, cancellationToken);
    }

    private async Task SendV8Sessions(Message message, CancellationToken cancellationToken)
    {
        var request =
            MessagePackSerializer.Deserialize<V8SessionsRequestDto>(message.Data, cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(request.Cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        var result = request.InfoBase switch
        {
            null => await rac.GetClusterSessions(
                request.Cluster.ClusterInternalId,
                request.Cluster.Credentials?.User ?? "",
                request.Cluster.Credentials?.Password ?? ""),
            _ => await rac.GetInfoBaseSessions(
                request.Cluster.ClusterInternalId,
                request.InfoBase.InfoBaseInternalId,
                request.Cluster.Credentials?.User ?? "",
                request.Cluster.Credentials?.Password ?? "",
                request.InfoBase.Credentials?.User ?? "",
                request.InfoBase.Credentials?.Password ?? "")
        };

        await _server.SendV8Sessions(message, result, cancellationToken);
    }

    private async Task SendV8Processes(Message message, CancellationToken cancellationToken)
    {
        var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        var result = await rac.GetClusterProcesses(
            cluster.ClusterInternalId,
            cluster.Credentials?.User ?? "",
            cluster.Credentials?.Password ?? "");

        await _server.SendV8Processes(message, result, cancellationToken);
    }

    private async Task ChangeInfoBaseParameters(Message message, CancellationToken cancellationToken)
    {
        var request =
            MessagePackSerializer.Deserialize<ChangeClusterObjectParametersRequestDto<InfoBaseDto>>(message.Data,
                cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(request.Item.Cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        await rac.UpdateInfoBaseParameters(
            request.Item.Cluster.ClusterInternalId,
            request.Item.InfoBaseInternalId,
            request.Parameters,
            request.Item.Cluster.Credentials?.User ?? "",
            request.Item.Cluster.Credentials?.Password ?? "",
            request.Item.Credentials?.User ?? "",
            request.Item.Credentials?.Password ?? "");

        await _server.Ok(message, cancellationToken);
    }

    private async Task SendRagentServices(Message message, CancellationToken cancellationToken)
    {
        var services = _v8ServicesProvider.GetRagentServices();
        await _server.SendRagentServices(message, services, cancellationToken);
    }

    private async Task SendRasServices(Message message, CancellationToken cancellationToken)
    {
        var services = _rasHolder.GetRasServices();
        await _server.SendRasServices(message, services, cancellationToken);
    }

    private async Task SendCrServerServices(Message message, CancellationToken cancellationToken)
    {
        var services = _v8ServicesProvider.GetCrServerServices();
        await _server.SendCrServerServices(message, services, cancellationToken);
    }

    private async Task SendEdtInstallations(Message message, CancellationToken cancellationToken)
    {
        var items = _edtPInstallationsProvider.GetInstallations();
        await _server.SendEdtInstallations(message, items.ToList(), cancellationToken);
    }

    private async Task CloseV8Sessions(Message message, CancellationToken cancellationToken)
    {
        var request =
            MessagePackSerializer.Deserialize<CloseV8SessionsRequestDto>(message.Data,
                cancellationToken: cancellationToken);

        var ragent = _v8ServicesProvider.GetActiveRagentByPort(request.Cluster.RagentPort);
        var ras = _rasHolder.GetActiveRasForRagent(ragent);
        var rac = Rac.GetRacForRasService(_racLogger, ras);

        foreach (var requestSessionsId in request.SessionsIds ?? [])
            try
            {
                await rac.TerminateSession(
                    request.Cluster.ClusterInternalId,
                    requestSessionsId,
                    request.Cluster.Credentials?.User ?? "",
                    request.Cluster.Credentials?.Password ?? "");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Ошибка закрытия соединения");
            }

        await _server.Ok(message, cancellationToken);
    }

    private async Task SendConfigRepositoryDetails(Message message, CancellationToken cancellationToken)
    {
        var request =
            MessagePackSerializer.Deserialize<ConfigRepositoryDetailsRequestDto>(message.Data,
                cancellationToken: cancellationToken);
        var crServer = _v8ServicesProvider.GetCrServerForPort(request.CrServerPort);

        using var db = new ConfigRepositoryConnection(Path.Combine(crServer.Directory, request.Repository));

        var users = db.ReadUsers();
        
        var result = new ConfigRepositoryDetailsDto
        {
            Id = db.ReadId(),
            Platform = crServer.Platform,
            Users = users.Select(c => new ConfigRepositoryUserDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList(),
            LastVersion = db.ReadVersions(users).MaxBy(c => c.Number)?.Number ?? -1
        };

        await _server.SendConfigRepositoryDetails(message, result, cancellationToken);
    }

    private async Task SendSystemInfo(Message message, CancellationToken cancellationToken)
    {
        var info = new SystemInfoDto
        {
            HostName = Environment.MachineName,
            IpAddresses = (await Dns.GetHostEntryAsync(Dns.GetHostName(), cancellationToken)).AddressList
                .Where(c => c.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(c))
                .Select(c => c.ToString())
                .ToArray(),
            AgentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? ""
        };

        await _server.SendSystemInfo(message, info, cancellationToken);
    }
}