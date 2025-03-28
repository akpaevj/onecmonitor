using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Services;

public class ClustersInfoBasesDetector(
    AgentsConnectionsManager connectionsManager,
    IMapper mapper,
    IServiceProvider serviceProvider,
    ILogger<ClustersInfoBasesDetector> logger)
    : BackgroundService
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
                            var defaultClusterAdminCredentials = await appDbContext.Credentials
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.DefaultForClusters, token);

                            var defaultInfoBaseAdminCredentials = await appDbContext.Credentials
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.DefaultV8Admin, token);

                            var dbClusters = await appDbContext.Clusters.ToListAsync(token);
                            var agentClusters = await connection.GetV8Clusters(token);

                            dbClusters.ExceptBy(agentClusters.Select(c => c.Id), i => i.ClusterInternalId).ToList().ForEach(
                                c => appDbContext.Clusters.Remove(c));

                            agentClusters.ExceptBy(dbClusters.Select(c => c.ClusterInternalId), i => i.Id).ToList().ForEach(c =>
                            {
                                var item = mapper.Map<Cluster>(c);
                                item.Id = Guid.NewGuid();
                                item.AgentId = connection.AgentInstance!.Id;
                                item.CredentialsId = defaultClusterAdminCredentials?.Id;
                                
                                appDbContext.Clusters.Add(item);
                            });

                            agentClusters.ForEach(cluster =>
                            {
                                var dbCluster = dbClusters.FirstOrDefault(c => c.ClusterInternalId == cluster.Id);
                                if (dbCluster == null)
                                    return;

                                mapper.Map(cluster, dbCluster);
                                dbCluster.AgentId = connection.AgentInstance!.Id;

                                appDbContext.Clusters.Update(dbCluster);
                            });

                            await appDbContext.SaveChangesAsync(token);

                            var clusters = await appDbContext.Clusters.AsNoTracking().ToListAsync(token);

                            foreach (var cluster in clusters)
                            {
                                try
                                {
                                    var dbInfoBases = await appDbContext.InfoBases.Where(c => c.ClusterId == cluster.Id).ToListAsync(token);
                                    var agentInfoBases = await connection.GetV8InfoBasesSummaries(cluster, token);
                                    
                                    dbInfoBases.ExceptBy(agentInfoBases.Select(c => c.Id), i => i.InfoBaseInternalId).ToList().ForEach(
                                        c => appDbContext.InfoBases.Remove(c));

                                    agentInfoBases.ExceptBy(dbInfoBases.Select(c => c.InfoBaseInternalId), i => i.Id).ToList().ForEach(c =>
                                    {
                                        var item = mapper.Map<InfoBase>(c);
                                        item.Id = Guid.NewGuid();
                                        item.Name = item.InfoBaseName;
                                        item.ClusterId = cluster.Id;
                                        item.CredentialsId = defaultInfoBaseAdminCredentials?.Id;
                                        item.PublishAddress = "http://localhost";
                                        
                                        appDbContext.InfoBases.Add(item);
                                    });
                                    
                                    agentInfoBases.ForEach(infoBase =>
                                    {
                                        var dbInfoBase = dbInfoBases.FirstOrDefault(c => c.InfoBaseInternalId == infoBase.Id);
                                        if (dbInfoBase == null)
                                            return;
                                        
                                        mapper.Map(infoBase, dbInfoBase);
                                        dbInfoBase.ClusterId = cluster.Id;

                                        appDbContext.InfoBases.Update(dbInfoBase);
                                    });
                                }
                                catch (Exception e)
                                {
                                    logger.LogError(e, 
                                        $"Ошибка получения списка информационных баз. Агент: {connection.AgentInstance!.InstanceName}. Кластер: {cluster.Name}");
                                }
                            }

                            await appDbContext.SaveChangesAsync(token);
                            await appDbContext.Database.CommitTransactionAsync(stoppingToken);
                        }
                        catch (Exception e)
                        {
                            await appDbContext.Database.RollbackTransactionAsync(stoppingToken);
                            logger.LogError(e, $"Ошибка получения списка кластеров. Агент: {connection.AgentInstance!.InstanceName}");
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