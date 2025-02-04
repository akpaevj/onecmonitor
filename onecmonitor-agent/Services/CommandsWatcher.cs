using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.Models;
using OneSTools.Common.Platform;

namespace OnecMonitor.Agent.Services
{
    internal class CommandsWatcher : BackgroundService
    {
        private readonly OnecMonitorConnection _onecMonitorConnection;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<CommandsWatcher> _logger;

        public CommandsWatcher(IServiceProvider serviceProvider, ILogger<CommandsWatcher> logger) 
        {
            var scope = serviceProvider.CreateAsyncScope();
            _onecMonitorConnection = scope.ServiceProvider.GetRequiredService<OnecMonitorConnection>();
            _appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Start watching commands");

            await _onecMonitorConnection.SubscribeForCommands(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _onecMonitorConnection.ReadMessage(stoppingToken);

                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (message.Header.Type)
                    {
                        case MessageType.TechLogSeances:
                            await UpdateTechLogSeances(stoppingToken);
                            break;
                        case MessageType.InstalledPlatformsRequest:
                            await _onecMonitorConnection.SendInstalledPlatforms(message, stoppingToken);
                            break;
                        default:
                            throw new Exception("Received unexpected message type");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to read message: {ex}");
                }
            }
        }

        private async Task UpdateTechLogSeances(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Updating tech log seances");

            try
            {
                var seances = await _onecMonitorConnection.GetTechLogSeances(cancellationToken);

                await _appDbContext.Database.BeginTransactionAsync(cancellationToken);

                var currentSeances = await _appDbContext.TechLogSeances.ToListAsync(cancellationToken);
                var removedSeances = currentSeances.Where(c => seances.FirstOrDefault(e => e.Id == c.Id) == null).ToList();
                var addedSeances = seances.Where(c => currentSeances.FirstOrDefault(e => e.Id == c.Id) == null).ToList();
                var updatedSeances = currentSeances.Where(c =>
                {
                    var gotSeance = seances.FirstOrDefault(e => e.Id == c.Id);

                    if (gotSeance != null && c.Template != gotSeance.Template)
                    {
                        c.Template = gotSeance.Template;
                        return true;
                    }
                    else
                        return false;
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
