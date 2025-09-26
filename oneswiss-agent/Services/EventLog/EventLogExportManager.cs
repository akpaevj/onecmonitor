using Microsoft.Extensions.Caching.Memory;
using OneScript.Commons;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Helpers;
using OneSwiss.Common.Services;
using OneSwiss.V8.Platform.RemoteAdministration;
using Exception = System.Exception;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExportManager(
    V8ServicesProvider v8ServicesProvider,
    EventLogExporter exporter,
    ILogger<IEventLogReader> logReaderLogger,
    MonitorQueue<EventLogSettingsDto> settingsQueue,
    ILogger<EventLogExportManager> logger)
    : BackgroundService
{
    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;
    private readonly List<BracketsEventLogReader> _readers = [];

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var settings = await settingsQueue.DequeueAsync(cancellationToken);
                await InitFromSettings(settings, cancellationToken);
            
                DisposeReaders();

                if (!settings.Enabled) 
                    continue;
            
                foreach (var exportItem in settings.Items.Where(c => c.IsActive))
                {
                    var ragent = v8ServicesProvider.GetActiveRagentByPort(exportItem.InfoBase.Cluster.RagentPort);
                    var clusterCatalog = Path.Combine(ragent.WorkingDirectory, $"reg_{exportItem.InfoBase.Cluster.Port}");
                    var infoBaseLogCatalog = Path.Combine(clusterCatalog, exportItem.InfoBase.InfoBaseInternalId, "1Cv8Log");

                    var infoBaseInfo = new InfoBaseInfo(ragent.Platform, infoBaseLogCatalog,
                        exportItem.InfoBase.InfoBaseName, exportItem.InfoBase.InfoBaseInternalId, exportItem.Ttl);
                
                    var reader = new BracketsEventLogReader(infoBaseInfo, exporter, logReaderLogger);
                    _readers.Add(reader);
                
                    var position = await exporter.GetLastEventDateTime(infoBaseInfo.InfoBaseId, _cts!.Token);
                    reader.Start(position, _cts!.Token);
                }
            }
            catch (OperationCanceledException) {}
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обработки новых настроек экспорта журнала регистрации");
            }
        }
    }

    private async Task InitFromSettings(EventLogSettingsDto settings, CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _settings = settings;
        
        if (settings.Enabled)
            await exporter.Init(_settings.GetDbContext(), _settings, _cts.Token);
        else
            await exporter.FlushAsync(_cts.Token);
    }

    private void DisposeReaders()
    {
        _readers.ForEach(c => c.Dispose());
        _readers.Clear();
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            DisposeReaders();
        }
    }

    ~EventLogExportManager()
    {
        Dispose(false);
    }
}