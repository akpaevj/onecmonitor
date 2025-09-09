using System.Text.Json;
using LibGit2Sharp;
using OneSwiss.Agent.Helpers;
using OneSwiss.Common.DTO;
using OneSwiss.V8;
using OneSwiss.V8.Designer.Models;

namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTaskProcessor : IDisposable
{
    private readonly SemaphoreSlim _commitSemaphore = new(1);
    private readonly AgentsResourcesProvider _agentsResourcesProvider;
    private readonly string _ibcmdDataDirs;
    private readonly string _infoBasesPath;
    private readonly ILogger<GitSyncTaskItemProcessor> _itemLogger;
    private readonly Dictionary<Guid, GitSyncTaskItemProcessor> _itemsProcessors = new();
    private readonly ILogger<GitSyncTaskProcessor> _logger;
    private readonly string _repoFolder;
    private readonly OneSwissConnection _serverConnection;
    private readonly GitSyncTaskDto _task;
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
    }

    public string ProcessorFolder { get; }

    public async Task Start(CancellationToken stoppingToken)
    {
        try
        {
            await InitTaskProcessor(stoppingToken);
            await UpdateItemsInternal(_task.Items);
            StartPushing(_cts!.Token);
        }
        catch (OperationCanceledException) { }
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

            var foldersName = item.ExportFolder;
            
            var ibcmdDataFolder = Path.Combine(_ibcmdDataDirs, foldersName);
            var ibFolder = Path.Combine(_infoBasesPath, foldersName);
            var repoFolder = Path.Combine(_repoFolder, foldersName);

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
            }, ReadItemVersion, cts.Token);
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

    private async Task InitRepository()
    {
        if (!Directory.Exists(_repoFolder))
            Directory.CreateDirectory(_repoFolder);

        if (!Repository.IsValid(_repoFolder))
        {
            CloneRepository();
            
            await ProcessRunner.RunAndThrowAsync("git", ["checkout", "-b", _task.BranchName], _repoFolder);
            
            using var rep = new Repository(_repoFolder);
            SetHeadBranchTrackedBranch(rep);
            
            InitRepositoryConfig(rep);
            
            //PushChanges(rep);

            await InitLfs(_task.Items);

            InitGitIgnore();
            
            if (!rep.Commits.Any())
                InitCommit();
        }
    }

    private void SetHeadBranchTrackedBranch(Repository repo)
    {
        repo.Branches.Update(repo.Head, b =>
        {
            b.TrackedBranch = $"refs/remotes/origin/{_task.BranchName}";
        });
    }

    private void CreateRemoteBranchIfNeed(Repository repo)
    {
        if (!HasOutgoingChanges(repo))
            return;

        if (repo.Head.FriendlyName == _task.BranchName && repo.Head.TrackedBranch != null)
            return;
        
        var remote = repo.Network.Remotes.FirstOrDefault();
        var local = repo.Head;
        
        repo.Network.Push(remote, $"refs/heads/{local.FriendlyName}:refs/heads/{local.FriendlyName}", new PushOptions
        {
            CredentialsProvider = (_, _, _) => GetCredentials()
        });
    }

    private async Task InitLfs(List<GitSyncTaskItemDto> items)
    {
        foreach (var item in items.Where(item => item.LfsTrackers.Trim().Length > 0))
            foreach (var se in item.LfsTrackers.Split(','))
                await ProcessRunner.RunAsync("git", ["lfs", "track", $"{item.ExportFolder}/**/{se.Trim()}"],
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

        repository.MergeFetchedRefs(OneSwissSignature(), new MergeOptions
        {
            FastForwardStrategy = FastForwardStrategy.FastForwardOnly
        });
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
        await _commitSemaphore.WaitAsync();

        try
        {
            await WriteUploadVersion(itemProcessor, version);
            
            using var repo = new Repository(_repoFolder);

            var path = $"{Path.GetRelativePath(_repoFolder, itemProcessor.RepoFolder)}/*";
            Commands.Stage(repo, path);

            var metadataPath = Path.GetRelativePath(_repoFolder, GetItemMetadataPath(itemProcessor));
            Commands.Stage(repo, metadataPath);

            var author = new Signature(user.Name, user.GitUser, version.CreatedAt);

            repo.Commit(version.Comment, author, author, new CommitOptions
            {
                AllowEmptyCommit = true,
                PrettifyMessage = true
            });
        }
        finally
        {
            _commitSemaphore.Release();
        }
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
                    CreateRemoteBranchIfNeed(repository);
                    
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
        _logger.LogTrace("Начало отправки изменений в удаленный репозиторий");
        
        repository.Network.Push(repository.Head, new PushOptions
        {
            CredentialsProvider = (_, _, _) => GetCredentials(),
        });
        
        _logger.LogTrace("Отправка изменений в удаленный репозиторий окончена");
    }

    private async Task WriteUploadVersion(GitSyncTaskItemProcessor itemProcessor, ConfigRepositoryReportItem version)
    {
        var path = GetItemMetadataPath(itemProcessor);

        var metadata = File.Exists(path) switch
        {
            true => await ReadItemMetadata(itemProcessor),
            false => new GitSyncTaskItemMetadata
            {
                ExportFolder = itemProcessor.TaskItem.ExportFolder,
                IsExtension = itemProcessor.TaskItem.IsExtension,
                ConfigurationRepositoryId = itemProcessor.TaskItem.ConfigurationRepository.InternalId
            }
        };
        
        metadata.Version = version.Version;
        
        await WriteItemMetadata(itemProcessor, metadata);
    }
    
    private async Task<int> ReadItemVersion(GitSyncTaskItemProcessor item)
    {
        var path = GetItemMetadataPath(item);

        if (!File.Exists(path))
            return -1;
        
        var metadata = await ReadItemMetadata(item);
        return metadata.Version;
    }
    
    private async Task<GitSyncTaskItemMetadata> ReadItemMetadata(GitSyncTaskItemProcessor item)
    {
        var path = GetItemMetadataPath(item);
        return await ReadItemMetadata(path);
    }

    private static async Task<GitSyncTaskItemMetadata> ReadItemMetadata(string path)
    {
        await using var rStream = File.OpenRead(path);
        return JsonSerializer.Deserialize<GitSyncTaskItemMetadata>(rStream)!;
    }
    
    private async Task WriteItemMetadata(GitSyncTaskItemProcessor item, GitSyncTaskItemMetadata metadata)
    {
        var path = GetItemMetadataPath(item);
        await WriteItemMetadata(path, metadata);
    }
    
    private static async Task WriteItemMetadata(string path, GitSyncTaskItemMetadata metadata)
    {
        await using var wStream = new FileStream(path, FileMode.Create);
        
        await JsonSerializer.SerializeAsync(wStream, metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GetItemMetadataPath(GitSyncTaskItemProcessor item)
        => GetItemMetadataPath(item.TaskItem.ExportFolder);
    
    private string GetItemMetadataPath(string exportFolder)
        => Path.Combine(_repoFolder, $".{exportFolder}-metadata");

    public void Stop()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _commitSemaphore.Dispose();
        _cts?.Dispose();
    }
}