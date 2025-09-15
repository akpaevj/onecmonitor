using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Common.Services;
using OneSwiss.Server.Extensions;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.Services;

public class NewConfigRepositoryVersionHandler(
    AgentsConnectionsManager connectionsManager,
    IDbContextFactory<AppDbContext> contextFactory,
    IMapper mapper,
    MonitorQueue<(Guid RepoId, int Version)> newReposVersionsQueue,
    ILogger<NewConfigRepositoryVersionHandler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var item = await newReposVersionsQueue.DequeueAsync(stoppingToken);
            await CreateTasksByTemplates(item.RepoId, item.Version, stoppingToken);
        }
    }

    private async Task CreateTasksByTemplates(Guid configurationRepositoryId, int version, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var repository =
            await context.ConfigRepositories.FirstOrDefaultAsync(c => c.Id == configurationRepositoryId, cancellationToken);
        
        var templatesIds = await context.MaintenanceSteps
            .AsNoTracking()
            .Where(c => c.MaintenanceTask.IsTemplate && c.MaintenanceTask.StartWhenDiscoverNewConfigVersion)
            .Where(c =>
                c.LoadExtensionStep!.FromConfigRepository &&
                c.LoadExtensionStep.ConfigurationRepositoryId == configurationRepositoryId ||
                c.LoadConfigurationStep!.FromConfigRepository &&
                c.LoadConfigurationStep.ConfigurationRepositoryId == configurationRepositoryId)
            .Select(c => c.MaintenanceTaskId)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var templateId in templatesIds)
        {
            // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
            var template = (await context.MaintenanceTasks
                .AsNoTracking()
                .Include(c => c.Agents)
                .Include(c => c.InfoBases)
                .IncludeSteps()
                .FirstOrDefaultAsync(c => c.Id == templateId, cancellationToken: cancellationToken))!;
                
            var actionDescription = $"Хранилище {repository}. Версия {version}. Шаблон {template.Description}";
            
            try
            {
                var task = new MaintenanceTask();
                
                logger.LogTrace("Создание новой задачи для новой версии. {ActionDescription}", actionDescription);

                task.Description = $"{template.Description} (создана автоматически)";
                task.IsTemplate = false;
                task.StartWhenDiscoverNewConfigVersion = false;
                task.CommonDestination = template.CommonDestination;

                task.Agents = template.Agents;
                context.AttachRange(task.Agents);

                task.InfoBases = template.InfoBases;
                context.AttachRange(task.InfoBases);

                var newSteps = mapper.Map<List<MaintenanceStep>>(template.Steps);
                
                newSteps.ForEach(c =>
                {
                    c.Id = Guid.NewGuid();

                    if (c.LoadConfigurationStep?.FromConfigRepository == true &&
                        c.LoadConfigurationStep?.ConfigurationRepositoryId == configurationRepositoryId)
                    {
                        c.LoadConfigurationStep.LoadExactVersion = true;
                        c.LoadConfigurationStep.Version = version;
                    }
                    
                    if (c.LoadExtensionStep?.FromConfigRepository == true &&
                        c.LoadExtensionStep?.ConfigurationRepositoryId == configurationRepositoryId)
                    {
                        c.LoadExtensionStep.LoadExactVersion = true;
                        c.LoadExtensionStep.Version = version;
                    }
                });

                task.Steps.AddRange(newSteps);

                await context.MaintenanceTasks.AddAsync(task, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                try
                {
                    await connectionsManager.StartMaintenanceTask(task.Id, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Ошибка запуска автоматически созданной задачи обслуживания. {actionDescription}");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Ошибка автоматического создания задачи обслуживания. {actionDescription}");
            }
        }
    }
}