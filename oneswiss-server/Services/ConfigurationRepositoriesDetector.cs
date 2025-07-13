using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Services;

public class ConfigurationRepositoriesDetector(
    AgentsConnectionsManager connectionsManager,
    IMapper mapper,
    IServiceProvider serviceProvider,
    ILogger<ClustersInfoBasesDetector> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var connections = await connectionsManager.GetActiveAgentsConnections(stoppingToken);

            if (connections.Count > 0)
            {
                try
                {
                    await Parallel.ForEachAsync(connections, stoppingToken, async (connection, token) =>
                    {
                        await using var scope = serviceProvider.CreateAsyncScope();
                        await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        
                        await appDbContext.Database.BeginTransactionAsync(token);

                        try
                        {
                            /*var defaultAdmin = await appDbContext.Credentials
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.DefaultConfigRepositoryAdmin, token);
                            
                            var crServices = await connection.GetCrServerServices(token);
                            
                            var receivedIds = crServices.SelectMany(c => c.InternalIds.Values).ToList();
                            var existIds = await appDbContext.ConfigurationRepositories.AsNoTracking().Select(c => c.Id).ToListAsync(token);

                            var addedIds = receivedIds.Except(existIds);
                            var removedIds = existIds.Except(receivedIds);
                            var toUpdateIds = receivedIds.Intersect(existIds);

                            var removed = await appDbContext.ConfigurationRepositories
                                .Where(c => removedIds.Contains(c.Id))
                                .ToListAsync(token);
                            appDbContext.ConfigurationRepositories.RemoveRange(removed);*/

                            await appDbContext.SaveChangesAsync(token);
                            await appDbContext.Database.CommitTransactionAsync(token);
                        }
                        catch (Exception e)
                        {
                            await appDbContext.Database.RollbackTransactionAsync(token);
                            logger.LogError(e, $"Ошибка получения списка хранилищ конфигураций. Агент: {connection.AgentInstance!.InstanceName}");
                        }
                    });
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                
                await Task.Delay(60 * 1000, stoppingToken);
            }
            else
                await Task.Delay(5 * 1000, stoppingToken);
        }
    }
}