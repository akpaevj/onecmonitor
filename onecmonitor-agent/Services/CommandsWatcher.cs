using MessagePack;
using OnecMonitor.Agent.Services.EventLog;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;
using OnecMonitor.Common.Services;
using OneSTools.Common.Platform.RemoteAdministration;

namespace OnecMonitor.Agent.Services
{
    internal class CommandsWatcher
    {
        private readonly OnecMonitorConnection _server;
        private readonly MonitorQueue<MaintenanceTaskDto> _maintenanceTasksQueue;
        private readonly TechLogRepositoryManager _techLogRepositoryManager;
        private readonly EventLogExportManager _eventLogExportManager;
        private readonly RasHolder _rasHolder;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<CommandsWatcher> _logger;
        private readonly V8PlatformsProvider _v8PlatformsProvider;
        private readonly V8ServicesProvider _v8ServicesProvider;

        public CommandsWatcher(
            IServiceProvider serviceProvider,
            TechLogRepositoryManager techLogRepositoryManager,
            MonitorQueue<MaintenanceTaskDto> maintenanceTasksQueue,
            EventLogExportManager eventLogExportManager,
            RasHolder rasHolder,
            IHostApplicationLifetime appLifetime,
            V8PlatformsProvider v8PlatformsProvider,
            V8ServicesProvider v8ServicesProvider,
            ILogger<CommandsWatcher> logger) 
        {
            var scope = serviceProvider.CreateAsyncScope();
            _rasHolder = rasHolder;
            _server = scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
            _techLogRepositoryManager = techLogRepositoryManager;
            _v8PlatformsProvider = v8PlatformsProvider;
            _v8ServicesProvider = v8ServicesProvider;
            _eventLogExportManager = eventLogExportManager;
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
                    case MessageType.InfoBasesRequest:
                        await SendV8InfoBases(message, _applicationLifetime.ApplicationStopping);
                        break;
                    case MessageType.RagentServicesRequest:
                        await SendRagentServices(message, _applicationLifetime.ApplicationStopping);
                        break;
                    case MessageType.RasServicesRequest:
                        await SendRasServices(message, _applicationLifetime.ApplicationStopping);
                        break;
                    case MessageType.Settings:
                        await HandleSettings(message, _applicationLifetime.ApplicationStopping);
                        break;
                    case MessageType.MaintenanceTask:
                        await HandleMaintenanceTask(message, _applicationLifetime.ApplicationStopping);
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

        private async Task ApplySettings(SettingsDto settingsDto, CancellationToken cancellationToken)
        {
            _techLogRepositoryManager.SetSettings(settingsDto.TechLogSettings);
            await _eventLogExportManager.UpdateSettings(settingsDto.EventLogSettings, cancellationToken);
        }
        
        private async Task HandleSettings(Message message, CancellationToken cancellationToken)
        {
            var settings = MessagePackSerializer.Deserialize<SettingsDto>(message.Data, cancellationToken: cancellationToken);
            await ApplySettings(settings, cancellationToken);
            
            await _server.SendOk(message, cancellationToken);
        }

        private async Task UpdateSettings(CancellationToken cancellationToken)
        {
            var response = await _server.Get<SettingsDto>(
                MessageType.SettingsRequest,
                MessageType.Settings,
                cancellationToken);

            await ApplySettings(response, cancellationToken);
        }
        
        private async Task SendV8Clusters(Message message, CancellationToken cancellationToken)
        {
            var ragents = _v8ServicesProvider.GetActiveRagentServices();
            var clusters = new List<V8Cluster>();

            foreach (var rac in ragents.Select(_rasHolder.GetActiveRasForRagent).Select(Rac.GetRacForRasService))
                clusters.AddRange(rac.GetClusters());
            
            await _server.Send(MessageType.ClustersResponse, clusters, message, cancellationToken);
        }
        
        private async Task SendV8InfoBases(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<InfoBasesRequestDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = _v8ServicesProvider.GetActiveRagentForClusterPort(request.Cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(ras);

            var creds = request.Cluster.Credentials;
            var infoBases = rac.GetInfoBasesSummaries(request.Cluster.Id, creds?.User ?? "", creds?.Password ?? "");
            
            await _server.Send(MessageType.InfoBasesResponse, infoBases, message,
                cancellationToken);
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
    }
}
