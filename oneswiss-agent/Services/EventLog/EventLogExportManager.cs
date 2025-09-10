using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Services;
using OneSwiss.V8.Platform.RemoteAdministration;
using Exception = System.Exception;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExportManager : IDisposable
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SemaphoreSlim _clstLock = new(1, 1);
    private readonly Dictionary<string, ClstWatcher> _clstWatchers = new();
    private readonly EventLogExporter _exporter;
    private readonly ILogger<EventLogExportManager> _logger;
    private readonly ILogger<Rac> _racLogger;
    private readonly RasHolder _rasHolder;
    private readonly Dictionary<string, EventLogReader> _readers = new();
    private readonly SemaphoreSlim _readersLock = new(1, 1);
    private readonly EventLogRepositoryManager _repositoryManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly V8ServicesProvider _v8ServicesProvider;

    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;

    public EventLogExportManager(
        V8ServicesProvider v8ServicesProvider,
        RasHolder rasHolder,
        EventLogExporter exporter,
        IServiceProvider serviceProvider,
        EventLogRepositoryManager repositoryManager,
        IHostApplicationLifetime applicationLifetime,
        ILogger<EventLogExportManager> logger,
        ILogger<Rac> racLogger)
    {
        _v8ServicesProvider = v8ServicesProvider;
        _rasHolder = rasHolder;
        _exporter = exporter;
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _racLogger = racLogger;
        _repositoryManager = repositoryManager;

        _repositoryManager.SettingsChanged += SettingsChanged;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
                if (_clstWatchers.ContainsKey(ragent.WorkingDirectory))
                    continue;

                var ras = _rasHolder.GetActiveRasForRagent(ragent);
                var rac = Rac.GetRacForRasService(_racLogger, ras);
                var clusters = await rac.GetClusters();

                foreach (var cluster in clusters)
                {
                    var clusterCatalog = Path.Combine(ragent.WorkingDirectory, $"reg_{cluster.Port}");

                    var watcher = new ClstWatcher(ragent, cluster, clusterCatalog, _settings!.InfoBaseNameRegex);
                    watcher.InfoBasesAdded += OnInfoBasesAdded;
                    watcher.InfoBasesDeleted += OnInfoBasesDeleted;

                    try
                    {
                        _clstWatchers.TryAdd(clusterCatalog, watcher);
                        watcher.Watch();
                    }
                    catch (Exception e)
                    {
                        if (_clstWatchers.Remove(clusterCatalog, out _))
                            watcher.Dispose();

                        _logger.LogError(e, "Error while adding cluster to watcher");
                    }
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
            var reader = new EventLogReader(e, _exporter,
                _serviceProvider.GetRequiredService<ILogger<EventLogReader>>());
            reader.ProcessExited += (_, _) => RemoveReader(e.LogPath);

            _readers.TryAdd(e.LogPath, reader);

            reader.Start();
        }

        _readersLock.Release();
    }

    private void OnInfoBasesDeleted(object? sender, InfoBaseInfo e)
    {
        RemoveReader(e.LogPath);
    }

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

        if (disposing) _cts?.Dispose();
    }

    ~EventLogExportManager()
    {
        Dispose(false);
    }
}