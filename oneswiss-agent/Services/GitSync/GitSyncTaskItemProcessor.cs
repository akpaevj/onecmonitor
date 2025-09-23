using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Designer.Models;
using OneSwiss.V8.Platform;
using Org.BouncyCastle.Cmp;

namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTaskItemProcessor(
    V8Platform platform,
    V8Platform? basePlatform,
    GitSyncTaskItemDto item,
    string dataFolder,
    string ibFolder,
    string repoFolder,
    ILogger<GitSyncTaskItemProcessor> logger)
    : IDisposable
{
    private string _extensionName = string.Empty;
    
    private readonly string _baseRepoConnectionString = item.BaseConfigurationRepository != null ?
        $"tcp://{item.BaseConfigurationRepository.Host}:{item.BaseConfigurationRepository.Port}/{item.BaseConfigurationRepository.Name}" : string.Empty;
    
    private readonly string _repoConnectionString =
        $"tcp://{item.ConfigurationRepository.Host}:{item.ConfigurationRepository.Port}/{item.ConfigurationRepository.Name}";

    private CancellationTokenSource? _tcs;

    public EventHandler<Exception>? Stopped;
    public string RepoFolder { get; } = repoFolder;
    public string IbFolder { get; } = ibFolder;
    public GitSyncTaskItemDto TaskItem { get; } = item;

    public async Task Start(Func<VersionUploadedArgs, Task> versionUploadedFunc, Func<GitSyncTaskItemProcessor, Task<int>> readVersionFunc, CancellationToken stoppingToken)
    {
        try
        {
            await InitItemProcessor();

            _tcs = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            while (!_tcs.IsCancellationRequested)
            {
                var version = await readVersionFunc(this);
                if (version == -1)
                    version = TaskItem.ConfigurationRepositoryVersion;
                else
                    version += 1;
                
                var versions = await ReadVersions(version, _tcs.Token);

                foreach (var configRepositoryVersion in versions)
                {
                    ThrowIfCancelled();

                    // Находим email пользователя для фиксации коммита
                    var user = TaskItem.ConfigurationRepository.Users
                        .FirstOrDefault(c =>
                            c.Name.Equals(configRepositoryVersion.User, StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrEmpty(user!.GitUser?.Trim()))
                        throw new Exception(
                            $"Для пользователя {configRepositoryVersion.User} не установлено соответствие пользователя Git");

                    // Выгружаем версию в файлы
                    await DumpConfiguration(versionUploadedFunc, configRepositoryVersion, user);
                }

                await Task.Delay(10 * 1000, _tcs.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка обработки версий хранилища");
            Stopped?.Invoke(this, e);
        }
    }

    public void Stop()
    {
        _tcs?.Cancel();
    }

    private async Task DumpConfiguration(Func<VersionUploadedArgs, Task> versionUploadedFunc,
        ConfigRepositoryReportItem version,
        ConfigRepositoryUserDto user)
    {
        if (!string.IsNullOrEmpty(_baseRepoConnectionString))
        {
            try
            {
                using var batch = OnecV8BatchMode.CreateDesignerBatch(basePlatform!, IbFolder);
            
                logger.LogTrace($"Начало загрузки версии базовой конфигурации из хранилища - {TaskItem.ExportFolder}");
            
                await batch.UpdateConfigFromRepository(
                    _baseRepoConnectionString,
                    TaskItem.BaseConfigurationRepository!.Credentials?.User ?? "",
                    TaskItem.BaseConfigurationRepository.Credentials?.Password ?? "",
                    string.Empty,
                    string.Empty,
                    version.Version);
            
                logger.LogTrace($"Загрузка версии базовой конфигурации из хранилища окончена - {TaskItem.ExportFolder}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обновления базовой конфигурации из хранилища");
                throw;
            }
        }
        
        try
        {
            using var batch = OnecV8BatchMode.CreateDesignerBatch(platform, IbFolder);
            
            logger.LogTrace($"Начало загрузки версии конфигурации из хранилища - {TaskItem.ExportFolder}");
            
            await batch.UpdateConfigFromRepository(
                _repoConnectionString,
                TaskItem.ConfigurationRepository.Credentials?.User ?? "",
                TaskItem.ConfigurationRepository.Credentials?.Password ?? "",
                string.Empty,
                string.Empty,
                version.Version,
                _extensionName);
            
            logger.LogTrace($"Загрузка версии конфигурации из хранилища окончена - {TaskItem.ExportFolder}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка обновления конфигурации из хранилища");
            throw;
        }
        
        ThrowIfCancelled();
        
        try
        {
            logger.LogTrace("Начало выгрузки файлов конфигурации - {ItemExportFolder}", TaskItem.ExportFolder);
            
            await IbcmdWrapper.ExportXmlFiles(platform, dataFolder, IbFolder, RepoFolder, _extensionName);
            
            logger.LogTrace("Выгрузка файлов конфигурации окончена - {ItemExportFolder}", TaskItem.ExportFolder);
            
            logger.LogTrace("Начало фиксации изменений в git - {ItemExportFolder}", TaskItem.ExportFolder);

            await versionUploadedFunc(new VersionUploadedArgs
            {
                ReportItem = version,
                User = user
            });
            
            logger.LogTrace("Фиксация изменений в git окончена - {ItemExportFolder}", TaskItem.ExportFolder);

            ThrowIfCancelled();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка выгрузки конфигурации в файлы");
            throw;
        }
    }

    private async Task<List<ConfigRepositoryReportItem>> ReadVersions(int version, CancellationToken cancellationToken)
    {
        var result = new List<ConfigRepositoryReportItem>();

        try
        {
            using var batch = OnecV8BatchMode.CreateDesignerBatch(platform, IbFolder);

            var reader = await batch.GetConfigRepositoryReportReader(
                _repoConnectionString,
                TaskItem.ConfigurationRepository.Credentials?.User ?? "",
                TaskItem.ConfigurationRepository.Credentials?.Password ?? "",
                version,
                _extensionName);

            while (!reader.EndOfFile)
                result.Add(await reader.NextItem(false, cancellationToken));

            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка чтения отчета по версиям");
            throw;
        }
    }

    private async Task InitItemProcessor()
    {
        if (TaskItem.IsExtension)
            _extensionName = "EXT";
        
        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);

        if (!Directory.Exists(RepoFolder))
            Directory.CreateDirectory(RepoFolder);

        await InitFileBase();
    }

    private async Task InitFileBase()
    {
        if (!Directory.Exists(IbFolder))
            Directory.CreateDirectory(IbFolder);

        if (Directory.GetFiles(IbFolder).Length == 0)
        {
            await OnecV8BatchMode.CreateFileInfoBase(platform, IbFolder);

            if (TaskItem.IsExtension)
                await IbcmdWrapper.AddExtension(platform, dataFolder, IbFolder, _extensionName, "UL");
        }
    }

    private void ThrowIfCancelled()
    {
        if (_tcs?.IsCancellationRequested == true)
            throw new OperationCanceledException();
    }

    public void Dispose()
    {
        _tcs?.Dispose();
    }
    
    public class VersionUploadedArgs
    {
        public ConfigRepositoryReportItem ReportItem { get; set; }
        public ConfigRepositoryUserDto User { get; set; }
    }
}