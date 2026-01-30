using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Xml;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;

namespace OneSwiss.Agent.Services.TechLog;

public class TechLogManager
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TechLogFoldersManager _foldersManager;
    private readonly ILogger<TechLogManager> _logger;
    private readonly V8PlatformsProvider _platformsProvider;
    private readonly string _rootLogPath;

    private CancellationTokenSource? _cts;

    public TechLogManager(
        FilesProvider filesProvider,
        TechLogRepositoryManager repositoryManager,
        V8PlatformsProvider platformsProvider,
        TechLogFoldersManager foldersManager,
        IHostApplicationLifetime applicationLifetime,
        ILogger<TechLogManager> logger)
    {
        _rootLogPath = filesProvider.TechLogFolder;

        _platformsProvider = platformsProvider;
        _foldersManager = foldersManager;
        _applicationLifetime = applicationLifetime;
        _logger = logger;

        repositoryManager.SettingsChanged += SettingsChanged;
    }

    private async void SettingsChanged(object? sender, TechLogSettingsDto settings)
    {
        try
        {
            if (_cts != null)
                await _cts.CancelAsync();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.ApplicationStopping);

            await _foldersManager.Init(settings, _cts.Token);

            if (settings.Enabled)
                _ = Start(settings, _cts.Token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка обработки изменения настроек хранилища технологического журнала");
        }
    }

    private async Task Start(TechLogSettingsDto settings, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var logCfgPath in GetLogCfgPaths())
            {
                var logCfgContentBuilder = new StringBuilder("<config xmlns=\"http://v8.1c.ru/v8/tech-log\">\n");

                var builder = new StringBuilder();
                var logPaths = new List<string>();

                settings.Seances.ToList().ForEach(c =>
                {
                    if (c.FinishDateTime < DateTime.UtcNow)
                        return;

                    var logPath = Path.Combine(_rootLogPath, c.Id.ToString(), c.TemplateId.ToString());

                    try
                    {
                        if (!Directory.Exists(logPath))
                        {
                            var directory = Directory.CreateDirectory(logPath);

                            try
                            {
                                SetLogFolderAccessRights(directory);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Ошибка назначения прав на каталог сбора технологического журнала");
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Ошибка создания каталога сеанса сбора технологического журнала");
                        return;
                    }

                    var templateDoc = new XmlDocument();
                    templateDoc.LoadXml(c.Template);

                    var root = templateDoc.DocumentElement;
                    if (root == null)
                        return;

                    logPaths.Add(logPath);

                    root.SetAttribute("location", logPath);
                    root.SetAttribute("placement", "plain");

                    builder.AppendLine(templateDoc.OuterXml);
                });

                var removedPaths = _foldersManager.LogFolders.Except(logPaths);
                foreach (var removedPath in removedPaths)
                    await _foldersManager.RemoveFolder(removedPath, cancellationToken);

                var newPaths = logPaths.Except(_foldersManager.LogFolders);
                foreach (var newPath in newPaths)
                    await _foldersManager.AddFolder(newPath, cancellationToken);

                logCfgContentBuilder.AppendLine(builder.ToString());
                logCfgContentBuilder.AppendLine("</config>");

                if (logPaths.Count > 0)
                    await WriteTextToFile(logCfgPath, logCfgContentBuilder.ToString(), cancellationToken);
                else
                    DeleteFile(logCfgPath, cancellationToken);
            }

            await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void SetLogFolderAccessRights(DirectoryInfo directory)
    {
        if (!directory.Exists) directory.Create();

#pragma warning disable CA1416

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            File.SetUnixFileMode(directory.FullName,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
        }
        else
        {
            var security = directory.GetAccessControl();

            security.AddAccessRule(
                new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    FileSystemRights.Modify,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.InheritOnly,
                    AccessControlType.Allow));

            directory.SetAccessControl(security);
        }

#pragma warning restore CA1416
    }

    private string[] GetLogCfgPaths()
    {
        var paths = _platformsProvider
            .GetExistsPlatformInstallationPaths();

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            paths = [.. paths.Select(c => Path.GetDirectoryName(c)!).Distinct()];

        // Where в данном случае обязательно, т.к. если путь установки версий платформы отличается от стандартного, то conf будет только в одном каталоге
        return [.. paths
            .Select(c => Path.Combine(c, "conf", "logcfg.xml"))
            .Where(c => Directory.Exists(Path.GetDirectoryName(c)))];
    }

    private async Task WriteTextToFile(string path, string text, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await File.WriteAllTextAsync(path, text, cancellationToken);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Ошибка записи logcfg.xml ({path}))");
            }
    }

    private void DeleteFile(string path, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Ошибка удаления logcfg.xml ({path}))");
            }
    }
}