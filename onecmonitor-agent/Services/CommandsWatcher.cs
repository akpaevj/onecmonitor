using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Agent.Services
{
    internal class CommandsWatcher : BackgroundService
    {
        private readonly AsyncServiceScope _scope;
        private readonly ServerConnection _serverConnection;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<CommandsWatcher> _logger;

        public CommandsWatcher(ServerConnection serverConnection, IServiceProvider serviceProvider, ILogger<CommandsWatcher> logger) 
        {
            _serverConnection = serverConnection;
            _scope = serviceProvider.CreateAsyncScope();
            _appDbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Start watching commands");

            await _serverConnection.SubscribeForCommands(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await _serverConnection.ReadMessage(stoppingToken);

                switch (message.Header.Type)
                {
                    case MessageType.TechLogSeances:
                        await UpdateTechLogSeances(stoppingToken);
                        break;
                    default:
                        throw new Exception("Received unespected message type");
                }
            }
        }

        public async Task UpdateTechLogSeances(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Updating tech log seances");

            try
            {
                var seances = await _serverConnection.GetTechLogSeances(cancellationToken);

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
