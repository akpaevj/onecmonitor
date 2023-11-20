using System.Text;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Agent.Models;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Agent.Services
{
    public class TechLogSeancesWatcher : BackgroundService
    {
        private readonly string _logFolder = string.Empty;
        private readonly string _logCfgPath = string.Empty;

        private readonly AsyncServiceScope _scope;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TechLogSeancesWatcher> _logger;

        public TechLogSeancesWatcher(
            IServiceProvider serviceProvider, 
            IConfiguration configuration, 
            ILogger<TechLogSeancesWatcher> logger)
        {
            _scope = serviceProvider.CreateAsyncScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger = logger;

            _logFolder = configuration.GetValue("Techlog:LogFolder", "")!;
            if (string.IsNullOrEmpty(_logFolder))
                throw new Exception("Tech log folder path is not specified");

            _logCfgPath = configuration.GetValue("Techlog:LogCfg", "")!;
            if (string.IsNullOrEmpty(_logCfgPath))
                throw new Exception("logcfg.xml path is not specified");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    await _dbContext.Database.BeginTransactionAsync(stoppingToken);

                    var notStartedSeances = await _dbContext.TechLogSeances
                        .Where(c => c.StartDateTime <= now && c.Status == TechLogSeanceStatus.Sheduled).ToListAsync(stoppingToken);
                    notStartedSeances.ForEach(c => c.Status = TechLogSeanceStatus.Started);

                    _dbContext.TechLogSeances.UpdateRange(notStartedSeances);

                    if (notStartedSeances.Count > 0)
                        _logger.LogTrace($"Started seances: {notStartedSeances.Count}");

                    var notFinishedSeances = await _dbContext.TechLogSeances
                        .Where(c => c.FinishDateTime <= now && c.Status == TechLogSeanceStatus.Started).ToListAsync(stoppingToken);
                    notFinishedSeances.ForEach(c => c.Status = TechLogSeanceStatus.Finished);

                    _dbContext.TechLogSeances.UpdateRange(notFinishedSeances);

                    if (notFinishedSeances.Count > 0)
                        _logger.LogTrace($"Finished seances: {notFinishedSeances.Count}");

                    var deletedSeances = await _dbContext.TechLogSeances
                        .Where(c => c.Status == TechLogSeanceStatus.Deleted).ToListAsync(stoppingToken);

                    var deletedCount = 0;

                    foreach (var deletedSeance in deletedSeances)
                    {
                        try
                        {
                            var seanceFolder = Path.Combine(_logFolder, deletedSeance.Id.ToString());

                            if (Directory.Exists(seanceFolder))
                                Directory.Delete(seanceFolder, true);

                            _dbContext.Entry(deletedSeance).State = EntityState.Deleted;

                            deletedCount++;
                        }
                        catch { }
                    }

                    if (deletedCount > 0)
                        _logger.LogTrace($"Deleted seances: {deletedCount}");

                    var startedSeances = await _dbContext.TechLogSeances
                        .Where(c => c.Status == TechLogSeanceStatus.Started).ToListAsync(stoppingToken);

                    if (startedSeances.Count == 0)
                        DeleteFile(_logCfgPath, stoppingToken);
                    else
                    {
                        var logCfgContentBuilder = new StringBuilder("<config xmlns=\"http://v8.1c.ru/v8/tech-log\">\n");

                        startedSeances.ForEach(c =>
                        {
                            var log = c.Template.Replace("{LOG_PATH}", Path.Combine(_logFolder, c.Id.ToString()) + Path.DirectorySeparatorChar);

                            logCfgContentBuilder.AppendLine(log);
                        });

                        logCfgContentBuilder.AppendLine("</config>");

                        await WriteTextToFile(logCfgContentBuilder.ToString(), _logCfgPath, stoppingToken);
                    }

                    await _dbContext.Database.CommitTransactionAsync(stoppingToken);

                    await _dbContext.SaveChangesAsync(stoppingToken);
                }
                catch(Exception ex)
                {
                    await _dbContext.Database.RollbackTransactionAsync(stoppingToken);

                    _logger.LogError(ex, "Failed to handle tech log collecting seances");
                }

                _dbContext.ChangeTracker.Clear();

                // read the table every second
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task WriteTextToFile(string text, string path, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await File.WriteAllTextAsync(path, text, cancellationToken);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write logcfg");
                }
            }
        }

        private void DeleteFile(string path, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) 
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    break;
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete logcfg");
                }
            }
        }
    }
}
