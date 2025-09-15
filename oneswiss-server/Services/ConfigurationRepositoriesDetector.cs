using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Services;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services;

public class ConfigurationRepositoriesDetector(
    AgentsConnectionsManager connectionsManager,
    MonitorQueue<(Guid RepoId, int Version)> newReposVersionsQueue,
    IServiceProvider serviceProvider,
    ILogger<ConfigurationRepositoriesDetector> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var connections = await connectionsManager.GetActiveAgentsConnections(stoppingToken);

            if (connections.Count > 0)
            {
                var reposWithNewVersions = new ConcurrentBag<(Guid, int)>();
                
                try
                {
                    await Parallel.ForEachAsync(connections, stoppingToken, async (connection, token) =>
                    {
                        await using var scope = serviceProvider.CreateAsyncScope();
                        await using var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var removedReps = await appDbContext.ConfigRepositories
                            .Where(c => c.AgentId == connection.AgentInstance!.Id)
                            .Select(c => c.InternalId)
                            .ToListAsync(token);

                        try
                        {
                            var defaultAdmin = await appDbContext.Credentials
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.DefaultConfigRepositoriesAdmin, token);

                            var sysInfo = await connection.GetSystemInfo(token);
                            var crServices = await connection.GetCrServerServices(token);

                            foreach (var crService in crServices)
                            {
                                logger.LogDebug(
                                    $"Попытка получения хранилищ конфигураций из {connection.AgentInstance!.Id}:{crService.Name}");

                                foreach (var repository in crService.Repositories)
                                {
                                    var details =
                                        await connection.GetConfigRepositoryDetails(crService.Port, repository, token);

                                    removedReps.Remove(details.Id);

                                    var foundRep =
                                        await appDbContext.ConfigRepositories.FirstOrDefaultAsync(
                                            c => c.InternalId == details.Id, token);

                                    if (foundRep == null)
                                    {
                                        var id = Guid.NewGuid();

                                        await appDbContext.ConfigRepositories.AddAsync(new ConfigurationRepository
                                        {
                                            Id = id,
                                            InternalId = details.Id,
                                            Name = repository,
                                            Host = sysInfo.HostName,
                                            Port = crService.Port,
                                            AgentId = connection.AgentInstance!.Id,
                                            CredentialsId = defaultAdmin?.Id,
                                            LastReadVersion = details.LastVersion
                                        }, token);
                                        
                                        reposWithNewVersions.Add((id, details.LastVersion));

                                        foreach (var configUser in details.Users)
                                            await appDbContext.ConfigRepositoryUsers.AddAsync(
                                                new ConfigurationRepositoryUser
                                                {
                                                    Name = configUser.Name,
                                                    InternalId = configUser.Id,
                                                    RepositoryId = id
                                                }, token);
                                    }
                                    else
                                    {
                                        var lastReadVersion = foundRep.LastReadVersion;
                                        
                                        foundRep.Name = repository;
                                        foundRep.Host = sysInfo.HostName;
                                        foundRep.Port = crService.Port;
                                        foundRep.AgentId = connection.AgentInstance!.Id;
                                        foundRep.LastReadVersion = details.LastVersion;

                                        if (details.LastVersion != 0 && details.LastVersion > lastReadVersion)
                                            foreach (var version in Enumerable.Range(lastReadVersion + 1, details.LastVersion - lastReadVersion))
                                                reposWithNewVersions.Add((foundRep.Id, version));

                                        // Сначала отметим удаленных
                                        var existIds = details.Users.Select(c => c.Id).ToList();
                                        var deletedUsers = await appDbContext.ConfigRepositoryUsers
                                            .Where(c => c.Repository.AgentId == connection.AgentInstance.Id &&
                                                        !existIds.Contains(c.Id))
                                            .ToListAsync(token);
                                        deletedUsers.ForEach(c => c.Deleted = true);

                                        // теперь добавим новых и обновим существующих
                                        foreach (var configUser in details.Users)
                                        {
                                            var foundUser = await appDbContext.ConfigRepositoryUsers
                                                .FirstOrDefaultAsync(c => c.InternalId == configUser.Id,
                                                    token);
                                            if (foundUser == null)
                                                await appDbContext.ConfigRepositoryUsers.AddAsync(
                                                    new ConfigurationRepositoryUser
                                                    {
                                                        InternalId = configUser.Id,
                                                        Name = configUser.Name,
                                                        RepositoryId = foundRep.Id,
                                                        Deleted = false,
                                                        GitUser = string.Empty,
                                                        Id = Guid.NewGuid()
                                                    }, token);
                                            else
                                                foundUser.Name = configUser.Name;
                                        }
                                    }
                                }
                            }

                            var deleted = await appDbContext.ConfigRepositories
                                .Where(c => removedReps.Contains(c.InternalId)).ToListAsync(token);
                            deleted.ForEach(c => c.Deleted = true);

                            await appDbContext.SaveChangesAsync(token);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e,
                                $"Ошибка получения списка хранилищ конфигураций. Агент: {connection.AgentInstance!.InstanceName}");
                        }
                    });
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }

                foreach (var repoWithNewVersion in reposWithNewVersions)
                    await HandleNewRepositoryVersion(repoWithNewVersion.Item1, repoWithNewVersion.Item2, stoppingToken);

                await Task.Delay(60 * 1000, stoppingToken);
            }
            else
            {
                await Task.Delay(5 * 1000, stoppingToken);
            }
        }
    }
    
    private async Task HandleNewRepositoryVersion(Guid configurationRepositoryId, int version, CancellationToken cancellationToken)
        => await newReposVersionsQueue.QueueAsync((configurationRepositoryId, version), cancellationToken);
}