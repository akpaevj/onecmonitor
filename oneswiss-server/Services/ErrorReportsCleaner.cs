using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Services;

public class ErrorReportsCleaner(IServiceProvider serviceProvider, ILogger<ErrorReportsCleaner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var settings = dbContext.ErrorLoggingServiceSettings.FirstOrDefault();

            if (settings?.ReportsTtl > 0)
            {
                var deleteFrom = DateTime.Now.AddDays(-settings.ReportsTtl);

                try
                {
                    await dbContext.ErrorReports
                        .Where(c => c.CreatedAt <= deleteFrom)
                        .ExecuteDeleteAsync(stoppingToken);

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка очистки отчетов об ошибках");
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}