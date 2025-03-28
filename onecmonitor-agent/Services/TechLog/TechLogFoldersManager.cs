using System.Collections.Concurrent;
using System.Timers;
using OnecMonitor.Common.DTO;
using OneScript.Commons;
using Timer = System.Timers.Timer;

namespace OnecMonitor.Agent.Services.TechLog;

public class TechLogFoldersManager : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TechLogReadersManager _readersManager;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<TechLogFoldersManager> _logger;
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, TechLogFolderWatcher> _watchers = new();
    private readonly Timer? _deletingTimer;
    private readonly List<string> _foldersForDeleting = [];

    public IReadOnlyList<string> LogFolders => _watchers.Keys.ToList().AsReadOnly();
    
    public async Task Init(TechLogSettingsDto settings, CancellationToken cancellationToken)
        => await _readersManager.Init(settings, cancellationToken);

    public TechLogFoldersManager(
        IServiceProvider serviceProvider, 
        TechLogReadersManager readersManager,
        IHostApplicationLifetime applicationLifetime,
        ILogger<TechLogFoldersManager> logger)
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        
        _readersManager = readersManager;
        _readersManager.ReadingFinished += ReadingFinished;
        
        _logger = logger;

        _deletingTimer = new Timer(30 * 1000);
        _deletingTimer.Elapsed += DeletingTimerOnElapsed;
        _deletingTimer.Enabled = true;
    }

    private void DeletingTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _semaphore.Wait();
            
        for (var i = _foldersForDeleting.Count - 1; i >= 0; i--)
        {
            var folder = _foldersForDeleting[i];
                    
            try
            {
                Directory.Delete(folder, true);
                _foldersForDeleting.Remove(folder);
                _logger.LogTrace($"Каталог шаблона {folder} удален");
                
                var seanceFolder = Path.GetDirectoryName(folder)!;

                if (Directory.GetDirectories(seanceFolder).Length == 0)
                {
                    Directory.Delete(seanceFolder);
                    _logger.LogTrace($"Каталог сеанса {seanceFolder} удален");
                }
            }
            catch
            {
                // ignored
            }
        }

        _semaphore.Release();
    }

    public async Task AddFolder(string path, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!_watchers.ContainsKey(path))
            {
                var watcher = new TechLogFolderWatcher(path,
                    _serviceProvider.GetRequiredService<ILogger<TechLogFolderWatcher>>());
                watcher.FileChanged += WatcherOnFileChanged;

                _watchers.TryAdd(path, watcher);

                var directoryInfo = new DirectoryInfo(path);

                var files = directoryInfo
                    .GetFiles("*.log", SearchOption.AllDirectories)
                    .OrderByDescending(c => c.CreationTime).Select(c => c.FullName).ToArray();

                foreach (var file in files)
                    await AddReader(file, cancellationToken);

                watcher.Watch();
                _logger.LogTrace($"Наблюдатель каталога {path} добавлен");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ошибка добавления каталога {path} к чтению");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void WatcherOnFileChanged(object? sender, string path)
    {
        try
        {
            await _semaphore.WaitAsync();
        
            await AddReader(path, _applicationLifetime.ApplicationStopping);
        
            _semaphore.Release();
        }
        catch
        {
            // ignore
        }
    }
    
    private async void ReadingFinished(object? sender, string path)
    {
        try
        {
            await _semaphore.WaitAsync(_applicationLifetime.ApplicationStopping);

            try
            {
                await _readersManager.RemoveReader(path, _applicationLifetime.ApplicationStopping);
                
                var folder = Path.GetDirectoryName(path)!;

                if (_watchers.TryGetValue(folder, out var watcher))
                    watcher.RemoveFileFromStopList(path);
                else
                    _logger.LogWarning(
                        $"Файл {path} не удален из стоп-листа наблюдателя, т.к. наблюдатель для каталога журнала не найден");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка удаления файла {path} из стоп-листа наблюдателя");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task AddReader(string path, CancellationToken cancellationToken)
    {
        try
        {
            await _readersManager.AddReader(path, cancellationToken);
            
            var folder = Path.GetDirectoryName(path)!;
        
            if (_watchers.TryGetValue(folder, out var watcher))
                watcher.AddFileToStopList(path);
            else
                _logger.LogWarning($"Файл {path} не добавлен в стоп-лист наблюдателя, т.к. наблюдатель для каталога журнала не найден");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Ошибка добавления файла {path} в стоп-лист наблюдателя");
        }
    }
    
    public async Task RemoveFolder(string path, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        if (_watchers.Remove(path, out var watcher))
        {
            _foldersForDeleting.Add(path);
            watcher.Dispose();
        }
        
        _semaphore.Release();
        
        _logger.LogTrace($"Наблюдение каталога {path} остановлено");
    }

    private void ReleaseUnmanagedResources()
    {
        _watchers.Values.ForEach(c => c.Dispose());
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        
        if (disposing)
        {
            _deletingTimer?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~TechLogFoldersManager()
    {
        Dispose(false);
    }
}