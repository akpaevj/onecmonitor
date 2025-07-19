using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using Exception = System.Exception;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExportManager : IDisposable
{
    private readonly V8ServicesProvider _v8ServicesProvider;
    private readonly EventLogExporter _exporter;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<EventLogExportManager> _logger;
    private readonly EventLogRepositoryManager _repositoryManager;
    
    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;
    private readonly SemaphoreSlim _clstLock = new(1, 1);
    private readonly Dictionary<string, ClstWatcher> _clstWatchers = new();
    private readonly SemaphoreSlim _readersLock = new(1, 1);
    private readonly Dictionary<string, EventLogReader> _readers = new();

    public EventLogExportManager(
        V8ServicesProvider v8ServicesProvider,
        EventLogExporter exporter,
        IServiceProvider serviceProvider,
        EventLogRepositoryManager repositoryManager,
        IHostApplicationLifetime applicationLifetime,
        ILogger<EventLogExportManager> logger)
    {
        _v8ServicesProvider = v8ServicesProvider;
        _exporter = exporter;
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _repositoryManager = repositoryManager;
            
        _repositoryManager.SettingsChanged += SettingsChanged;
    }
    
    private async Task SettingsChanged(EventLogSettingsDto settings)
    {
        if (_cts != null)
            await _cts.CancelAsync();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.ApplicationStopping);
        
        await ReleaseReadersAndWatchers(_cts.Token);

        _settings = settings;

        await _exporter.Init(_repositoryManager.GetInstance(), _settings, _cts.Token);
        
        if (_settings.Enabled)
            _ = Start(_cts.Token).ConfigureAwait(false);
    }

    private async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var ragentServices = _v8ServicesProvider.GetRagentServices();
            
            await _clstLock.WaitAsync(cancellationToken);
            
            foreach (var ragent in ragentServices)
            {
                if (_clstWatchers.ContainsKey(ragent.ClusterCatalog)) 
                    continue;
                
                var watcher = new ClstWatcher(ragent, _settings!.InfoBaseNameRegex);
                watcher.InfoBasesAdded += OnInfoBasesAdded;
                watcher.InfoBasesDeleted += OnInfoBasesDeleted;
                
                try
                {
                    _clstWatchers.TryAdd(ragent.ClusterCatalog, watcher);
                    watcher.Watch();
                }
                catch (Exception e)
                {
                    if (_clstWatchers.Remove(ragent.ClusterCatalog, out _))
                        watcher.Dispose();
                    
                    _logger.LogError(e, "Error while adding cluster to watcher");
                }
            }
            
            _clstLock.Release();

            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
        }
    }

    private void OnInfoBasesAdded(object? sender, InfoBaseInfo e)
    {
        _readersLock.Wait();

        if (!_readers.ContainsKey(e.LogPath))
        {
            var reader = new EventLogReader(e, _exporter, _serviceProvider.GetRequiredService<ILogger<EventLogReader>>());
            reader.ProcessExited += (_, _) => RemoveReader(e.LogPath);
            
            _readers.TryAdd(e.LogPath, reader);
        
            _ = reader.Start();
        }

        _readersLock.Release();
    }
    
    private void OnInfoBasesDeleted(object? sender, InfoBaseInfo e)
        => RemoveReader(e.LogPath);

    private void RemoveReader(string logPath)
    {
        _readersLock.Wait();
        
        if (_readers.Remove(logPath, out var reader))
            reader.Dispose();
        
        _readersLock.Release();
    }

    private async Task ReleaseReadersAndWatchers(CancellationToken cancellationToken)
    {
        await _clstLock.WaitAsync(cancellationToken);
        await _readersLock.WaitAsync(cancellationToken);

        ReleaseReadersAndWatchers();
        
        _clstLock.Release();
        _readersLock.Release();
    }

    private void ReleaseReadersAndWatchers()
    {
        _clstWatchers.ForEach(c => c.Value.Dispose());
        _readers.ForEach(c => c.Value.Dispose());
        
        _clstWatchers.Clear();
        _readers.Clear();
    }

    private void ReleaseUnmanagedResources()
    {
        ReleaseReadersAndWatchers();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        
        if (disposing)
        {
            _cts?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~EventLogExportManager()
    {
        Dispose(false);
    }
}