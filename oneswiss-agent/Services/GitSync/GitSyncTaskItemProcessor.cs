using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Designer.Models;
using OneSwiss.V8.Platform;
using Org.BouncyCastle.Cmp;

namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTaskItemProcessor(
    V8Platform platform,
    GitSyncTaskItemDto item,
    string dataFolder,
    string ibFolder,
    string repoFolder,
    ILogger<GitSyncTaskItemProcessor> logger)
    : IDisposable
{
    private string _extensionName = string.Empty;
    private readonly string _repoConnectionString =
        $"tcp://{item.ConfigurationRepository.Host}:{item.ConfigurationRepository.Port}/{item.ConfigurationRepository.Name}";

    private CancellationTokenSource? _tcs;

    public EventHandler<Exception>? Stopped;
    public Guid Id { get; } = item.ConfigurationRepository.InternalId;
    public string RepoFolder { get; } = repoFolder;
    public string IbFolder { get; } = ibFolder;
    public string ExportFolder { get; } = item.ExportFolder;
    public bool IsExtension { get; } = item.IsExtension;

    public async Task Start(Func<VersionUploadedArgs, Task> versionUploadedFunc, Func<Guid, Task<int>> readVersionFunc, CancellationToken stoppingToken)
    {
        try
        {
            await InitItemProcessor();

            _tcs = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            while (!_tcs.IsCancellationRequested)
            {
                var version = await readVersionFunc(item.Id);
                var versions = await ReadVersions(version, _tcs.Token);

                foreach (var configRepositoryVersion in versions)
                {
                    ThrowIfCancelled();

                    // Находим email пользователя для фиксации коммита
                    var user = item.ConfigurationRepository.Users
                        .FirstOrDefault(c =>
                            c.Name.Equals(configRepositoryVersion.User, StringComparison.InvariantCultureIgnoreCase));

                    if (string.IsNullOrEmpty(user!.GitUser?.Trim()))
                        throw new Exception(
                            $"Для пользователя {configRepositoryVersion.User} не установлено соответствие пользователя Git");

                    // Выгружаем версию в файлы
                    await DumpVersion(versionUploadedFunc, configRepositoryVersion, user);
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

    private async Task DumpVersion(Func<VersionUploadedArgs, Task> versionUploadedFunc,
        ConfigRepositoryReportItem version,
        ConfigRepositoryUserDto user)
    {
        if (item.IsExtension)
            await DumpConfiguration(versionUploadedFunc, version, user);
        else
            await DumpConfiguration(versionUploadedFunc, version, user);
    }

    private async Task DumpConfiguration(Func<VersionUploadedArgs, Task> versionUploadedFunc,
        ConfigRepositoryReportItem version,
        ConfigRepositoryUserDto user)
    {
        var batch = OnecV8BatchMode.CreateDesignerBatch(platform, IbFolder);

        try
        {
            batch.UpdateConfigFromRepository(
                _repoConnectionString,
                item.ConfigurationRepository.Credentials?.User ?? "",
                item.ConfigurationRepository.Credentials?.Password ?? "",
                string.Empty,
                string.Empty,
                version.Version,
                _extensionName);

            ThrowIfCancelled();
            
            await IbcmdWrapper.ExportXmlFiles(platform, dataFolder, IbFolder, RepoFolder, _extensionName);

            await versionUploadedFunc(new VersionUploadedArgs
            {
                ReportItem = version,
                User = user
            });

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

    private async Task DumpExtension(Func<VersionUploadedArgs, Task> versionUploadedFunc,
        ConfigRepositoryReportItem version,
        ConfigRepositoryUserDto user)
    {
        var batch = OnecV8BatchMode.CreateDesignerBatch(platform, IbFolder);

        try
        {
            var cfePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cfe");
            
            batch.DumpConfigRepository(
                cfePath,
                _repoConnectionString,
                item.ConfigurationRepository.Credentials?.User ?? "",
                item.ConfigurationRepository.Credentials?.Password ?? "",
                version.Version,
                _extensionName);

            ThrowIfCancelled();
            
            await IbcmdWrapper.ExportXmlFilesFromFile(platform, dataFolder, cfePath, RepoFolder);

            await versionUploadedFunc(new VersionUploadedArgs
            {
                ReportItem = version,
                User = user
            });

            ThrowIfCancelled();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка выгрузки расширения в файлы");
            throw;
        }
    }

    private async Task<List<ConfigRepositoryReportItem>> ReadVersions(int version, CancellationToken cancellationToken)
    {
        var result = new List<ConfigRepositoryReportItem>();

        try
        {
            var batch = OnecV8BatchMode.CreateDesignerBatch(platform, IbFolder);

            var reader = batch.GetConfigRepositoryReportReader(
                _repoConnectionString,
                item.ConfigurationRepository.Credentials?.User ?? "",
                item.ConfigurationRepository.Credentials?.Password ?? "",
                version > 0 ? ++version : version,
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
        if (item.IsExtension)
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
            OnecV8BatchMode.CreateFileInfoBase(platform, IbFolder);

            if (item.IsExtension)
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