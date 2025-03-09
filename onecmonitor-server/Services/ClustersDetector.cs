using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Services;

public class ClustersDetector : BackgroundService
{
    private readonly AsyncServiceScope _serviceScope;
    private readonly AppDbContext _appDbContext;
    private readonly AgentsConnectionsManager _connectionsManager;
    private readonly ILogger<ClustersDetector> _logger;

    public ClustersDetector(AgentsConnectionsManager connectionsManager, IServiceProvider serviceProvider,
        ILogger<ClustersDetector> logger)
    {
        _serviceScope = serviceProvider.CreateAsyncScope();
        _appDbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _connectionsManager = connectionsManager;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var defaultClusterAdminCredentials = await _appDbContext.Credentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.DefaultForClusters, stoppingToken);
            
            var defaultInfoBaseAdminCredentials = await _appDbContext.Credentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.DefaultV8Admin, stoppingToken);
            
            try
            {
                var agents = await _appDbContext.Agents.ToListAsync(stoppingToken);
                var connections = _connectionsManager.GetAgentsConnections(agents);

                await Parallel.ForEachAsync(connections, stoppingToken, async (connection, token) =>
                {
                    var clusters = await connection.GetV8Clusters(token);
                    var currentClusterIds = await _appDbContext.Clusters.Select(c => c.ClusterInternalId).ToListAsync(token);
                    var newClusters = clusters.Where(c => !currentClusterIds.Contains(c.Id)).ToList();

                    foreach (var newCluster in newClusters)
                    {
                        var cluster = new Cluster
                        {
                            Id = Guid.NewGuid(),
                            AgentId = connection.AgentInstance!.Id,
                            ClusterInternalId = newCluster.Id,
                            CredentialsId = defaultClusterAdminCredentials?.Id,
                            Name = newCluster.Name,
                            Host = newCluster.Host,
                            Port = newCluster.Port
                        };
                        
                        await _appDbContext.Clusters.AddAsync(cluster, token);
                        
                        var infoBases = await connection.GetV8InfoBasesSummaries(cluster, token);
                        var currentIds = await _appDbContext.InfoBases.Select(c => c.InfoBaseInternalId).ToListAsync(token);
                        var newInfoBases = infoBases.Where(c => !currentIds.Contains(c.Id)).ToList();
                    
                        foreach (var infoBase in newInfoBases)
                        {
                            await _appDbContext.InfoBases.AddAsync(new InfoBase
                            {
                                Id = Guid.NewGuid(),
                                Name = infoBase.Name,
                                InfoBaseName = infoBase.Name,
                                InfoBaseInternalId = infoBase.Id,
                                ClusterId = cluster.Id,
                                CredentialsId = defaultInfoBaseAdminCredentials?.Id,
                                PublishAddress = "http://localhost"
                            }, token);
                        }
                    }
                    
                    await _appDbContext.SaveChangesAsync(token);
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка получения списка кластеров и информационных баз");
            }
            
            await Task.Delay(60 * 1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _serviceScope.Dispose();
        base.Dispose();
    }
}