using System.Text.Json;
using LibGit2Sharp;
using OneSwiss.Agent.Helpers;
using OneSwiss.Common.DTO;
using OneSwiss.V8;
using OneSwiss.V8.Designer.Models;

namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTaskProcessor
{
    private readonly AgentsResourcesProvider _agentsResourcesProvider;
    private readonly string _ibcmdDataDirs;
    private readonly string _infoBasesPath;
    private readonly ILogger<GitSyncTaskItemProcessor> _itemLogger;
    private readonly Dictionary<Guid, GitSyncTaskItemProcessor> _itemsProcessors = new();
    private readonly ILogger<GitSyncTaskProcessor> _logger;
    private readonly string _repoFolder;
    private readonly OneSwissConnection _serverConnection;
    private readonly GitSyncTaskDto _task;
    private readonly string _taskMetadataPath;
    private CancellationTokenSource? _cts;
    public EventHandler<Exception>? Stopped;

    public GitSyncTaskProcessor(
        AgentsResourcesProvider agentsResourcesProvider,
        OneSwissConnection serverConnection,
        GitSyncTaskDto task,
        FilesProvider filesProvider,
        ILogger<GitSyncTaskProcessor> logger,
        ILogger<GitSyncTaskItemProcessor> itemLogger)
    {
        _agentsResourcesProvider = agentsResourcesProvider;
        _serverConnection = serverConnection;
        _task = task;
        _logger = logger;
        _itemLogger = itemLogger;

        ProcessorFolder = Path.Combine(filesProvider.GitSyncFolder, _task.GitRepository.Id.ToString());
        _infoBasesPath = Path.Combine(ProcessorFolder, "ib");
        _ibcmdDataDirs = Path.Combine(ProcessorFolder, "ibcmd");
        _repoFolder = Path.Combine(ProcessorFolder, "repo");
        _taskMetadataPath = Path.Combine(_repoFolder, ".metadata");
    }

    public string ProcessorFolder { get; }

    public async Task Start(CancellationToken stoppingToken)
    {
        try
        {
            await InitTaskProcessor(stoppingToken);
            await UpdateItemsInternal(_task.Items);
            //StartPushing(_cts!.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Stop();
            _logger.LogError(e, "Ошибка запуска обработчика задачи синхронизации");
            Stopped?.Invoke(this, e);
        }
    }

    public async Task UpdateItems(List<GitSyncTaskItemDto> items)
    {
        try
        {
            await InitLfs(items);
            await UpdateItemsInternal(items);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Stop();
            _logger.LogError(e, "Ошибка обновления элементов задачи синхронизации");
            Stopped?.Invoke(this, e);
        }
    }

    private async Task UpdateItemsInternal(List<GitSyncTaskItemDto> items)
    {
        await InitMetadata(items);

        // Удалим процессоры, которых вообще нет в пришедшем списке
        var toDelete = _itemsProcessors
            .Where(c => items.FirstOrDefault(i => i.Id == c.Key) == null)
            .ToDictionary();
        foreach (var item in toDelete)
        {
            item.Value.Stop();
            await IoHelper.DeleteFolderInCycle(item.Value.IbFolder, 5);
            await IoHelper.DeleteFolderInCycle(item.Value.RepoFolder, 5);
            _itemsProcessors.Remove(item.Key);
        }

        // Остановим неактивные
        var toStop = _itemsProcessors
            .Where(c => items.FirstOrDefault(i => i.Id == c.Key)?.IsActive == false)
            .ToDictionary();
        foreach (var item in toStop)
        {
            item.Value.Stop();
            _itemsProcessors.Remove(item.Key);
        }

        var newItems = items.Where(c => !_itemsProcessors.ContainsKey(c.Id) && c.IsActive).ToList();

        foreach (var item in newItems)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts!.Token);

            var ibcmdDataFolder = Path.Combine(_ibcmdDataDirs, item.Id.ToString());
            var ibFolder = Path.Combine(_infoBasesPath, item.Id.ToString());
            var repoFolder = Path.Combine(_repoFolder, item.ExportFolder);

            var platform = await _agentsResourcesProvider.GetCrServerPlatform(item.ConfigurationRepository, _cts.Token);

            var itemProcessor =
                new GitSyncTaskItemProcessor(platform, item, ibcmdDataFolder, ibFolder, repoFolder, _itemLogger);

            // ReSharper disable once AsyncVoidMethod
            itemProcessor.Stopped += async void (_, args) =>
            {
                _itemsProcessors.Remove(item.Id);
                await SendItemProcessorStoppedNotification(item.ConfigurationRepository.Id, args.Message);
            };

            _itemsProcessors.Add(item.Id, itemProcessor);
            _ = itemProcessor.Start(async args =>
            {
                try
                {
                    await CommitChanges(itemProcessor, args.ReportItem, args.User);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ошибка фиксации изменений");

                    _itemsProcessors.Remove(item.Id);
                    itemProcessor.Stop();

                    await SendItemProcessorStoppedNotification(item.ConfigurationRepository.Id, e.Message);
                }
            },ReadItemVersion, cts.Token);
        }
    }

    private async Task SendItemProcessorStoppedNotification(Guid configurationRepositoryId, string message)
    {
        try
        {
            await _serverConnection.NotifyGitSyncTaskItemProcessorStopped(_task.Id, configurationRepositoryId, message,
                CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка отправки уведомления об остановке синхронизации хранилища");
        }
    }

    private async Task InitTaskProcessor(CancellationToken cancellation)
    {
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

        if (!Directory.Exists(ProcessorFolder))
            Directory.CreateDirectory(ProcessorFolder);

        if (Directory.Exists(_infoBasesPath))
            Directory.CreateDirectory(_infoBasesPath);

        await InitRepository();
    }

    private async Task<int> ReadItemVersion(Guid itemId)
    {
        var metadata = await ReadTaskMetadata();
        return (int)metadata!.Items.FirstOrDefault(c => c.Id == itemId)?.Version!;
    }

    private async Task InitMetadata(List<GitSyncTaskItemDto> items)
    {
        GitSyncTaskMetadata? oldMetadata = null;
        if (File.Exists(_taskMetadataPath))
            oldMetadata = await ReadTaskMetadata();

        var metadata = new GitSyncTaskMetadata();

        items.ForEach(c =>
        {
            var version = oldMetadata == null
                ? c.ConfigurationRepositoryVersion
                : oldMetadata.Items.FirstOrDefault(i => i.Id == c.Id)?.Version ?? c.ConfigurationRepositoryVersion;

            metadata.Items.Add(new GitSyncTaskItemMetadata
            {
                Id = c.Id,
                Version = version,
                ExportFolder = c.ExportFolder,
                IsExtension = c.IsExtension
            });
        });

        await using var wStream = new FileStream(_taskMetadataPath, FileMode.Create);
        await JsonSerializer.SerializeAsync(wStream, metadata);
    }

    private async Task InitRepository()
    {
        if (!Directory.Exists(_repoFolder))
            Directory.CreateDirectory(_repoFolder);

        if (!Repository.IsValid(_repoFolder))
        {
            CloneRepository();

            var rep = new Repository(_repoFolder);

            await InitLfs(_task.Items);

            InitGitIgnore();
            InitRepositoryConfig(rep);
            
            if (!rep.Commits.Any())
                InitCommit();
        }
    }

    private async Task InitLfs(List<GitSyncTaskItemDto> items)
    {
        await ProcessRunner.RunAsync("git", string.Join(" ", "lfs", "install"), _repoFolder);

        foreach (var item in items.Where(item => item.LfsTrackers.Trim().Length > 0))
        foreach (var se in item.LfsTrackers.Split(','))
            await ProcessRunner.RunAsync("git", string.Join(" ", "lfs", "track", $"{item.ExportFolder}/**/{se.Trim()}"),
                _repoFolder);
    }

    private void CloneRepository()
    {
        Repository.Clone(_task.GitRepository.Address, _repoFolder, new CloneOptions
        {
            FetchOptions =
            {
                CredentialsProvider = (_, _, _) => GetCredentials()
            }
        });
    }

    private void InitGitIgnore()
    {
        File.WriteAllLines(Path.Combine(_repoFolder, ".gitignore"),
        [
            "*/ConfigDumpInfo.xml"
        ]);
    }

    private static void InitRepositoryConfig(Repository repository)
    {
        repository.Config.Set("core.quotepath", false);
        repository.Config.Set("gui.encoding", "utf-8");
        repository.Config.Set("i18n.commitEncoding", "utf-8");
        repository.Config.Set("diff.renameLimit", 1);
        repository.Config.Set("diff.renames", false);
    }

    private UsernamePasswordCredentials GetCredentials()
    {
        return new UsernamePasswordCredentials
        {
            Username = string.Empty,
            Password = _task.GitRepository.Token.Token
        };
    }

    private void PullChanges(Repository repository)
    {
        var options = new FetchOptions
        {
            CredentialsProvider = (_, _, _) => GetCredentials()
        };

        var remote = repository.Network.Remotes.First();
        var refSpecs = remote.FetchRefSpecs.Select(c => c.Specification);

        Commands.Fetch(repository, remote.Name, refSpecs, options, "Обновление из удаленного репозитория");

        /*repository.MergeFetchedRefs(new Signature("oneswiss", "oneswiss", DateTime.Now), new MergeOptions
        {
            FastForwardStrategy = FastForwardStrategy.FastForwardOnly
        });*/
    }
    
    private void InitCommit()
    {
        using var repo = new Repository(_repoFolder);
        Commands.Stage(repo, "*");

        var author = OneSwissSignature();

        repo.Commit("Инициализация репозитория", author, author, new CommitOptions
        {
            AllowEmptyCommit = true,
            PrettifyMessage = true
        });
    }

    private static Signature OneSwissSignature()
        => new("OneSwiss", "OneSwiss", DateTime.Now);

    private async Task CommitChanges(GitSyncTaskItemProcessor itemProcessor, ConfigRepositoryReportItem version,
        ConfigRepositoryUserDto user)
    {
        using var repo = new Repository(_repoFolder);

        var path = $"{Path.GetRelativePath(_repoFolder, itemProcessor.RepoFolder)}/*";
        Commands.Stage(repo, path);

        var author = new Signature(user.Name, user.GitUser, version.CreatedAt);

        repo.Commit(version.Comment, author, author, new CommitOptions
        {
            AllowEmptyCommit = true,
            PrettifyMessage = true
        });

        await WriteUploadVersion(itemProcessor, version);
    }

    private void StartPushing(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var repository = new Repository(_repoFolder);

                    if (HasOutgoingChanges(repository))
                        PushChanges(repository);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ошибка отправки изменений в удаленный репозиторий");
                }

                await Task.Delay(10 * 1000, cancellationToken);
            }
        }, cancellationToken);
    }

    private static bool HasOutgoingChanges(Repository repository)
    {
        var currentBranch = repository.Head;

        if (!currentBranch.IsTracking)
            return false;

        var filter = new CommitFilter
        {
            IncludeReachableFrom = currentBranch.Tip,
            ExcludeReachableFrom = currentBranch.TrackedBranch.Tip
        };

        return repository.Commits.QueryBy(filter).ToList().Count != 0;
    }

    private void PushChanges(Repository repository)
    {
        repository.Network.Push(repository.Head, new PushOptions
        {
            CredentialsProvider = (_, _, _) => GetCredentials()
        });
    }

    private async Task WriteUploadVersion(GitSyncTaskItemProcessor itemProcessor, ConfigRepositoryReportItem version)
    {
        var metadata = await ReadTaskMetadata();
        if (metadata == null)
            throw new Exception("Файл метаданных задачи не обнаружен");

        var metadataItem = metadata.Items.FirstOrDefault(c => c.Id == itemProcessor.Id);
        if (metadataItem == null)
            throw new Exception($"Элемент файла метаданных не обнаружен ({itemProcessor.Id})");

        metadataItem.ExportFolder = itemProcessor.ExportFolder;
        metadataItem.Version = version.Version;
        metadataItem.IsExtension = itemProcessor.IsExtension;

        await using var wStream = new FileStream(_taskMetadataPath, FileMode.Create);
        await JsonSerializer.SerializeAsync(wStream, metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private async Task<GitSyncTaskMetadata?> ReadTaskMetadata()
    {
        await using var rStream = File.OpenRead(_taskMetadataPath);
        return JsonSerializer.Deserialize<GitSyncTaskMetadata>(rStream);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}