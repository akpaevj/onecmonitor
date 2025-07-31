using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MessagePack;
using OneSwiss.Common.DTO;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Services;
using OneSwiss.V8.ConfigurationRepository;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Services
{
    internal class CommandsWatcher
    {
        private readonly OneSwissConnection _server;
        private readonly MonitorQueue<MaintenanceTaskDto> _maintenanceTasksQueue;
        private readonly TechLogRepositoryManager _techLogRepositoryManager;
        private readonly EventLogRepositoryManager _eventLogRepositoryManager;
        private readonly RasHolder _rasHolder;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<CommandsWatcher> _logger;
        private readonly V8PlatformsProvider _v8PlatformsProvider;
        private readonly V8ServicesProvider _v8ServicesProvider;
        private readonly EdtInstallationsProvider _edtPInstallationsProvider;
        private readonly ILogger<Rac> _racLogger;

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
            ILogger<Rac> racLogger) 
        {
            var scope = serviceProvider.CreateAsyncScope();
            _racLogger = racLogger;
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
            
            _applicationLifetime.ApplicationStopping.Register(() =>
            {
                _server.MessageReceived -= MessageReceived;
            });
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
            catch (OperationCanceledException) {}
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Ошибка обработки сообщения");
                await _server.SendError(message, ex.Message, _applicationLifetime.ApplicationStopping);
            }
        }

        private async Task HandleMaintenanceTask(Message message, CancellationToken cancellationToken)
        {
            var task = MessagePackSerializer.Deserialize<MaintenanceTaskDto>(message.Data, cancellationToken: cancellationToken);
            await _maintenanceTasksQueue.QueueAsync(task, cancellationToken);

            await _server.SendOk(message, cancellationToken);
        }

        public async Task Start(CancellationToken token)
        {
            await _server.Start(true);
            
            await UpdateSettings(token);
        }

        private async Task SendInstalledPlatforms(Message message, CancellationToken cancellationToken)
        {
            var platforms = _v8PlatformsProvider.GetInstalledPlatforms();
            await _server.Send(MessageType.InstalledPlatforms, platforms, message, cancellationToken);
        }

        private void ApplySettings(SettingsDto settingsDto, CancellationToken cancellationToken)
        {
            _techLogRepositoryManager.UpdateSettings(settingsDto.TechLogSettings);
            _eventLogRepositoryManager.UpdateSettings(settingsDto.EventLogSettings);
        }
        
        private async Task HandleSettings(Message message, CancellationToken cancellationToken)
        {
            var settings = MessagePackSerializer.Deserialize<SettingsDto>(message.Data, cancellationToken: cancellationToken);
            ApplySettings(settings, cancellationToken);
            
            await _server.SendOk(message, cancellationToken);
        }

        private async Task UpdateSettings(CancellationToken cancellationToken)
        {
            var response = await _server.Get<SettingsDto>(
                MessageType.SettingsRequest,
                MessageType.Settings,
                cancellationToken);

            ApplySettings(response, cancellationToken);
        }
        
        private async Task SendV8Clusters(Message message, CancellationToken cancellationToken)
        {
            var ragents = _v8ServicesProvider.GetActiveRagentServices();
            var clusters = new List<V8Cluster>();

            foreach (var rac in ragents.Select(_rasHolder.GetActiveRasForRagent).Select(c => Rac.GetRacForRasService(_racLogger, c)))
                clusters.AddRange(await rac.GetClusters());
            
            await _server.Send(MessageType.ClustersResponse, clusters, message, cancellationToken);
        }
        
        private async Task SendV8ClusterDetails(Message message, CancellationToken cancellationToken)
        {
            var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);
            
            var item = await rac.GetCluster(
                cluster.ClusterInternalId);
            
            await _server.Send(MessageType.ClusterDetailsResponse, item, message,
                cancellationToken);
        }
        
        private async Task ChangeClusterParameters(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<ChangeClusterObjectParametersRequestDto<ClusterDto>>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(request.Item.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);
            
            await rac.UpdateClusterParameters(request.Item.ClusterInternalId, request.Parameters);
            
            await _server.SendOk(message, cancellationToken);
        }
        
        private async Task SendV8InfoBases(Message message, CancellationToken cancellationToken)
        {
            var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);
            
            var infoBases = await rac.GetInfoBases(cluster.ClusterInternalId, cluster.Credentials?.User ?? "", cluster.Credentials?.Password ?? "");
            
            await _server.Send(MessageType.InfoBasesResponse, infoBases, message,
                cancellationToken);
        }
        
        private async Task SendV8InfoBaseDetails(Message message, CancellationToken cancellationToken)
        {
            var infoBase = MessagePackSerializer.Deserialize<InfoBaseDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(infoBase.Cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);
            
            var item = await rac.GetInfoBase(
                infoBase.Cluster.ClusterInternalId, 
                infoBase.InfoBaseInternalId, 
                infoBase.Cluster.Credentials?.User ?? "", 
                infoBase.Cluster.Credentials?.Password ?? "",
                infoBase.Credentials?.User ?? "",
                infoBase.Credentials?.Password ?? "");
            
            await _server.Send(MessageType.InfoBaseDetailsResponse, item, message,
                cancellationToken);
        }
        
        private async Task SendV8Sessions(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<V8SessionsRequestDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(request.Cluster.Port);
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
                    request.Cluster.Credentials?.Password ?? "")
            };
            
            await _server.Send(MessageType.V8SessionsResponse, result, message,
                cancellationToken);
        }
        
        private async Task SendV8Processes(Message message, CancellationToken cancellationToken)
        {
            var cluster = MessagePackSerializer.Deserialize<ClusterDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);
            
            var result = await rac.GetClusterProcesses(
                cluster.ClusterInternalId, 
                cluster.Credentials?.User ?? "", 
                cluster.Credentials?.Password ?? "");
            
            await _server.Send(MessageType.V8ProcessesResponse, result, message,
                cancellationToken);
        }
        
        private async Task ChangeInfoBaseParameters(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<ChangeClusterObjectParametersRequestDto<InfoBaseDto>>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(request.Item.Cluster.Port);
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
            
            await _server.SendOk(message, cancellationToken);
        }
        
        private async Task SendRagentServices(Message message, CancellationToken cancellationToken)
        {
            var services = _v8ServicesProvider.GetRagentServices();
            await _server.Send(MessageType.RagentServices, services, message, cancellationToken);
        }
        
        private async Task SendRasServices(Message message, CancellationToken cancellationToken)
        {
            var services = _rasHolder.GetRasServices();
            await _server.Send(MessageType.RasServices, services, message, cancellationToken);
        }
        
        private async Task SendCrServerServices(Message message, CancellationToken cancellationToken)
        {
            var services = _v8ServicesProvider.GetCrServerServices();
            await _server.Send(MessageType.CrServerServices, services, message, cancellationToken);
        }
        
        private async Task SendEdtInstallations(Message message, CancellationToken cancellationToken)
        {
            var items = _edtPInstallationsProvider.GetInstallations();
            await _server.Send(MessageType.EdtInstallations, items, message, cancellationToken);
        }
        
        private async Task CloseV8Sessions(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<CloseV8SessionsRequestDto>(message.Data, cancellationToken: cancellationToken);
            
            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(request.Cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(_racLogger, ras);

            foreach (var requestSessionsId in request.SessionsIds ?? [])
            {
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
            }
            
            await _server.SendOk(message, cancellationToken);
        }
        
        private async Task SendConfigRepositoryDetails(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<ConfigRepositoryDetailsRequestDto>(message.Data, cancellationToken: cancellationToken);
            var crServer = _v8ServicesProvider.GetCrServerForPort(request.CrServerPort);

            using var db = new ConfigRepositoryConnection(Path.Combine(crServer.Directory, request.Repository, "1cv8ddb.1CD"));

            var result = new ConfigRepositoryDetailsDto
            {
                Id = db.ReadId(),
                Users = db.ReadUsers().Select(c => new ConfigRepositoryUserDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList()
            };
            
            await _server.Send(MessageType.ConfigRepositoryDetails, result, message, cancellationToken);
        }
        
        private async Task SendSystemInfo(Message message, CancellationToken cancellationToken)
        {
            var info = new SystemInfoDto
            {
                HostName = Environment.MachineName,
                IpAddresses = (await Dns.GetHostEntryAsync(Dns.GetHostName(), cancellationToken)).AddressList
                    .Where(c => c.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(c))
                    .Select(c => c.ToString())
                    .ToArray()
            };
            await _server.Send(MessageType.SystemInfo, info, message, cancellationToken);
        }
    }
}
