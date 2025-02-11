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
        private readonly ILogger<CommandsWatcher> _logger;

        public CommandsWatcher(IServiceProvider serviceProvider, InfoBasesUpdater infoBasesUpdater, ILogger<CommandsWatcher> logger) 
        {
            var scope = serviceProvider.CreateAsyncScope();
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
                            case MessageType.V8ServicesRequest:
                                await SendV8Services(message, stoppingToken);
                                break;
                            case MessageType.UpdateInfoBasesRequest:
                                await HandleUpdateInfoBasesRequest(message, stoppingToken);
                                break;
                            default:
                                throw new Exception("Received unexpected message type");
                        }
                    }
                    catch (Exception e)
                    {
                        await _server.SendError(message, e.Message, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to read message: {ex}");
                }
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
        
        private async Task SendV8Clusters(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send clusters");
            
            var rac = Rac.CreateRacForLaunchedAgent();
            if (rac == null)
                throw new Exception("Failed to get RAC for launched agent");
            
            await _server.Send(MessageType.ClustersResponse, rac.GetClusters(), message, cancellationToken);
        }
        
        private async Task SendV8Services(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send services");
            
            var services = V8Services.GetV8Services();
            await _server.Send(MessageType.V8Services, services, message, cancellationToken);
        }
        
        private async Task SendV8InfoBases(Message message, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Send infobases");
            
            var request = MessagePackSerializer.Deserialize<InfoBasesRequestDto>(message.Data, cancellationToken: cancellationToken);
            
            var rac = Rac.CreateRacForLaunchedAgent();
            if (rac == null)
                throw new Exception("Failed to get RAC for launched agent");
            
            var infoBases = rac.GetInfoBasesSummaries(request.Cluster.Id);
            
            await _server.Send(MessageType.InfoBasesResponse, infoBases, message,
                cancellationToken);
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
