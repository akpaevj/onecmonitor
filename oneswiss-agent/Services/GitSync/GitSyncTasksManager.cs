using OneSwiss.Agent.Helpers;
using OneSwiss.Common.DTO;

namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTasksManager(
    AgentsResourcesProvider agentsResourcesProvider,
    MonitorQueue<List<GitSyncTaskDto>> queue,
    FilesProvider filesProvider,
    [FromKeyedServices(OneSwissConnection.CommonKey)]
    OneSwissConnection serverConnection,
    IHostApplicationLifetime lifetime,
    ILogger<GitSyncTaskProcessor> taskLogger,
    ILogger<GitSyncTaskItemProcessor> taskItemLogger,
    ILogger<GitSyncTasksManager> logger) : IDisposable
{
    private readonly Dictionary<Guid, GitSyncTaskProcessor> _processors = new();
    private CancellationTokenSource? _cts;
    private GitSyncSettingsDto? _settings;

    public void UpdateSettings(GitSyncSettingsDto settings)
    {
        if (settings.Enabled)
            _ = Start();
        else
            Stop();

        _settings = settings;
    }

    private async Task Start()
    {
        if (_settings?.Enabled == true)
            return;

        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);

        while (!_cts.Token.IsCancellationRequested)
        {
            var tasks = await queue.DequeueAsync(_cts.Token);

            try
            {
                // Удалим процессоры, которых вообще нет в пришедшем списке
                var toDelete = _processors
                    .Where(c => tasks.FirstOrDefault(i => i.Id == c.Key) == null)
                    .ToDictionary();
                foreach (var gitSyncTaskDto in toDelete)
                {
                    gitSyncTaskDto.Value.Stop();
                    await IoHelper.DeleteFolderInCycle(gitSyncTaskDto.Value.ProcessorFolder, 5);
                    _processors.Remove(gitSyncTaskDto.Key);
                }

                // Остановим неактивные
                var toStop = _processors
                    .Where(c => tasks.FirstOrDefault(i => i.Id == c.Key)?.IsActive == false)
                    .ToDictionary();
                foreach (var gitSyncTaskDto in toStop)
                {
                    gitSyncTaskDto.Value.Stop();
                    _processors.Remove(gitSyncTaskDto.Key);
                }

                // Обновим существуюшие
                foreach (var task in tasks)
                    if (_processors.TryGetValue(task.Id, out var taskProcessor))
                        await taskProcessor.UpdateItems(task.Items);

                var newTasks = tasks.Where(c => !_processors.ContainsKey(c.Id) && c.IsActive).ToList();
                foreach (var gitSyncTaskDto in newTasks)
                {
                    var tcs = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

                    var taskProcessor = new GitSyncTaskProcessor(
                        agentsResourcesProvider,
                        serverConnection,
                        gitSyncTaskDto,
                        filesProvider,
                        taskLogger,
                        taskItemLogger);

                    taskProcessor.Stopped += async void (_, args) =>
                    {
                        try
                        {
                            _processors.Remove(gitSyncTaskDto.Id);

                            await serverConnection.NotifyGitSyncTaskProcessorStopped(gitSyncTaskDto.Id, args.Message,
                                CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Ошибка отправки уведомления об остановке синхронизации хранилища");
                        }
                    };

                    _processors.Add(gitSyncTaskDto.Id, taskProcessor);
                    _ = taskProcessor.Start(tcs.Token);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Ошибка обработки задач синхронизации: {e.Message}");
            }
        }
    }

    private void Stop()
    {
        if (_settings?.Enabled == false)
            return;

        _cts?.Cancel();
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}