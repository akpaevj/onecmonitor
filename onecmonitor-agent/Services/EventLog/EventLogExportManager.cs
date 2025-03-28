using OnecMonitor.Common.DTO;
using OneScript.Commons;
using Exception = System.Exception;

namespace OnecMonitor.Agent.Services.EventLog;

public class EventLogExportManager(
    V8ServicesProvider v8ServicesProvider,
    EventLogExporter exporter,
    IServiceProvider serviceProvider,
    IHostApplicationLifetime applicationLifetime,
    ILogger<EventLogExportManager> logger) : IDisposable
{
    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;
    private readonly SemaphoreSlim _clstLock = new(1, 1);
    private readonly Dictionary<string, ClstWatcher> _clstWatchers = new();
    private readonly SemaphoreSlim _readersLock = new(1, 1);
    private readonly Dictionary<string, EventLogReader> _readers = new();

    private async Task Start(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var ragentServices = v8ServicesProvider.GetRagentServices();
            
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
                    
                    logger.LogError(e, "Error while adding cluster to watcher");
                }
            }
            
            _clstLock.Release();

            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
        }
    }
    
    public async Task UpdateSettings(EventLogSettingsDto settings, CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
        
        await ReleaseReadersAndWatchers(_cts.Token);

        _settings = settings;

        await exporter.Init(_settings, cancellationToken);
        
        if (_settings.Enabled)
            _ = Start(_cts.Token).ConfigureAwait(false);
    }

    private void OnInfoBasesAdded(object? sender, InfoBaseInfo e)
    {
        _readersLock.Wait();

        if (!_readers.ContainsKey(e.LogPath))
        {
            var reader = new EventLogReader(e, exporter, serviceProvider.GetRequiredService<ILogger<EventLogReader>>());
            reader.ProcessExited += (_, _) => RemoveReader(e.LogPath);
            
            _readers.TryAdd(e.LogPath, reader);
        
            reader.Start();
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