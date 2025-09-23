using Microsoft.Extensions.Caching.Memory;
using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Helpers;
using OneSwiss.Common.Services;
using OneSwiss.V8.Platform.RemoteAdministration;
using Exception = System.Exception;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExportManager : BackgroundService
{
    private readonly EventLogExporter _exporter;
    private readonly MonitorQueue<EventLogSettingsDto> _settingsQueue;
    private readonly V8ServicesProvider _v8ServicesProvider;
    private readonly ILogger<EventLogExportManager> _logger;
    private readonly ILogger<IEventLogReader> _logReaderLogger;

    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;

    public EventLogExportManager(
        V8ServicesProvider v8ServicesProvider,
        EventLogExporter exporter,
        ILogger<EventLogExportManager> logger,
        ILogger<IEventLogReader> logReaderLogger,
        MonitorQueue<EventLogSettingsDto> settingsQueue)
    {
        _v8ServicesProvider = v8ServicesProvider;
        _exporter = exporter;
        _logger = logger;
        _logReaderLogger = logReaderLogger;
        _settingsQueue = settingsQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var settings = await _settingsQueue.DequeueAsync(cancellationToken);
            await InitFromSettings(settings, cancellationToken);

            if (!settings.Enabled) 
                continue;
            
            foreach (var exportItem in settings.Items.Where(c => c.IsActive))
            {
                var ragent = _v8ServicesProvider.GetActiveRagentByPort(exportItem.InfoBase.Cluster.RagentPort);
                var clusterCatalog = Path.Combine(ragent.WorkingDirectory, $"reg_{exportItem.InfoBase.Cluster.Port}");
                var infoBaseLogCatalog = Path.Combine(clusterCatalog, exportItem.InfoBase.InfoBaseInternalId, "1Cv8Log");

                var infoBaseInfo = new InfoBaseInfo(ragent.Platform, infoBaseLogCatalog,
                    exportItem.InfoBase.InfoBaseName, exportItem.InfoBase.InfoBaseInternalId, exportItem.Ttl);
                
                var reader = new BracketsEventLogReader(infoBaseInfo, _exporter, _logReaderLogger);
                
                var position = await _exporter.GetLastEventDateTime(infoBaseInfo.InfoBaseId, _cts!.Token);
                reader.Start(position, _cts!.Token);
            }
        }
    }

    private async Task InitFromSettings(EventLogSettingsDto settings, CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _settings = settings;
        
        if (_settings.Enabled)
            await _exporter.Init(settings.GetDbContext(), _settings, _cts.Token);
    }

    private void Dispose(bool disposing)
    {
        if (disposing) 
            _cts?.Dispose();
    }

    ~EventLogExportManager()
    {
        Dispose(false);
    }
}