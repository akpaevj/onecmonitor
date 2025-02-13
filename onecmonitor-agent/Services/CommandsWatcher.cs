using MessagePack;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Agent.Services.InfoBases;
using OnecMonitor.Common.DTO;
using OneSTools.Common.Platform;

namespace OnecMonitor.Agent.Services
{
    internal class CommandsWatcher : BackgroundService
    {
        private readonly OnecMonitorConnection _server;
        private readonly AppDbContext _appDbContext;
        private readonly InfoBasesUpdater _infoBasesUpdater;
        private readonly RasHolder _rasHolder;
        private readonly ILogger<CommandsWatcher> _logger;

        public CommandsWatcher(IServiceProvider serviceProvider, InfoBasesUpdater infoBasesUpdater, RasHolder rasHolder, ILogger<CommandsWatcher> logger) 
        {
            var scope = serviceProvider.CreateAsyncScope();
            _rasHolder = rasHolder;
            _server = scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
            _appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _infoBasesUpdater = infoBasesUpdater;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Start watching commands");

            await _server.Send(MessageType.SubscribingForCommands, stoppingToken);
            
            await UpdateTechLogSeances(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _server.ReadMessage(stoppingToken);

                    try
                    {
                        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                        switch (message.Header.Type)
                        {
                            case MessageType.UpdateTechLogSeancesRequest:
                                await UpdateTechLogSeancesByRequest(message, stoppingToken);
                                break;
                            case MessageType.InstalledPlatformsRequest:
                                await SendInstalledPlatforms(message, stoppingToken);
                                break;
                            case MessageType.ClustersRequest:
                                await SendV8Clusters(message, stoppingToken);
                                break;
                            case MessageType.InfoBasesRequest:
                                await SendV8InfoBases(message, stoppingToken);
                                break;
                            case MessageType.RagentServicesRequest:
                                await SendRagentServices(message, stoppingToken);
                                break;
                            case MessageType.RasServicesRequest:
                                await SendRasServices(message, stoppingToken);
                                break;
                            case MessageType.UpdateInfoBasesRequest:
                                await HandleUpdateInfoBasesRequest(message, stoppingToken);
                                break;
                            case MessageType.UpdateSettingsRequest:
                                await HandleUpdateSettingsRequest(message, stoppingToken);
                                break;
                            default:
                                throw new Exception($"Получено неожиданное сообщение: {message.Header.Type}");
                        }
                    }
                    catch (OperationCanceledException) {}
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Ошибка обработки сообщения");
                        await _server.SendError(message, ex.Message, stoppingToken);
                    }
                }
                catch (OperationCanceledException) {}
            }
        }

        private async Task SendInstalledPlatforms(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send installed platforms");
            
            var platforms = V8Platforms.GetInstalledPlatforms();
            await _server.Send(MessageType.InstalledPlatforms, platforms, message, cancellationToken);
        }

        private async Task HandleUpdateInfoBasesRequest(Message message, CancellationToken cancellationToken)
        {
            await _server.SendOk(message, cancellationToken);
            _infoBasesUpdater.RequestInfoBasesUpdateTask();
        }
        
        private async Task HandleUpdateSettingsRequest(Message message, CancellationToken cancellationToken)
        {
            var request = MessagePackSerializer.Deserialize<UpdateSettingsRequestDto>(message.Data, cancellationToken: cancellationToken);
            await _server.SendOk(message, cancellationToken);
        }
        
        private async Task SendV8Clusters(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send clusters");

            var ragents = V8Services.GetActiveRagentServices();
            var clusters = new List<V8Cluster>();

            foreach (var rac in ragents.Select(_rasHolder.GetActiveRasForRagent).Select(Rac.GetRacForRasService))
                clusters.AddRange(rac.GetClusters());
            
            await _server.Send(MessageType.ClustersResponse, clusters, message, cancellationToken);
        }
        
        private async Task SendV8InfoBases(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send infobases");
            
            var request = MessagePackSerializer.Deserialize<InfoBasesRequestDto>(message.Data, cancellationToken: cancellationToken);

            var ragent = V8Services.GetActiveRagentForClusterPort(request.Cluster.Port);
            var ras = _rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(ras);
            
            var infoBases = rac.GetInfoBasesSummaries(request.Cluster.Id);
            
            await _server.Send(MessageType.InfoBasesResponse, infoBases, message,
                cancellationToken);
        }
        
        private async Task SendRagentServices(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send ragent services");
            
            var services = V8Services.GetRagentServices();
            await _server.Send(MessageType.RagentServices, services, message, cancellationToken);
        }
        
        private async Task SendRasServices(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send ras services");
            
            var services = _rasHolder.GetRasServices();
            await _server.Send(MessageType.RasServices, services, message, cancellationToken);
        }

        private async Task UpdateTechLogSeancesByRequest(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Updating tech log seances by server request");
            await _server.SendOk(message, cancellationToken);

            await UpdateTechLogSeances(cancellationToken);
        }

        private async Task UpdateTechLogSeances(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Updating tech log seances");

            try
            {
                var seances = await _server.Get<List<TechLogSeanceDto>>(
                    MessageType.TechLogSeancesRequest, 
                    MessageType.TechLogSeances, 
                    cancellationToken);;

                await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

                var currentSeances = await _appDbContext.TechLogSeances.ToListAsync(cancellationToken);
                var removedSeances = currentSeances.Where(c => seances.FirstOrDefault(e => e.Id == c.Id) == null).ToList();
                var addedSeances = seances.Where(c => currentSeances.FirstOrDefault(e => e.Id == c.Id) == null).ToList();
                var updatedSeances = currentSeances.Where(c =>
                {
                    var gotSeance = seances.FirstOrDefault(e => e.Id == c.Id);

                    if (gotSeance == null || c.Template == gotSeance.Template) 
                        return false;
                    
                    c.Template = gotSeance.Template;
                    return true;

                }).ToList();

                removedSeances.ForEach(c => c.Status = Models.TechLogSeanceStatus.Deleted);

                await _appDbContext.AddRangeAsync(addedSeances.Select(seance => new Models.TechLogSeance()
                {
                    Id = seance.Id,
                    StartDateTime = seance.StartDateTime,
                    FinishDateTime = seance.FinishDateTime,
                    Template = seance.Template
                }), cancellationToken);

                if (updatedSeances.Count > 0)
                    _appDbContext.UpdateRange(updatedSeances);

                await _appDbContext.Database.CommitTransactionAsync(cancellationToken);

                await _appDbContext.SaveChangesAsync(cancellationToken);

                _logger.LogTrace("Tech log seances updated");
            }
            catch (Exception ex)
            {
                await _appDbContext.Database.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update tech log collecting seances");
            }

            _appDbContext.ChangeTracker.Clear();
        }
    }
}
