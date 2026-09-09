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
    EventLogSettingsState settingsState,
    ILogger<EventLogExportManager> logger)
    : BackgroundService
{
    private CancellationTokenSource? _cts;
    private EventLogSettingsDto? _settings;
    private readonly List<BracketsEventLogReader> _readers = [];
    private readonly object _readersLock = new();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var settings = await settingsQueue.DequeueAsync(cancellationToken);
                settingsState.Current = settings;
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

                    var position = await exporter.GetLastEventDateTime(infoBaseInfo.InfoBaseId, _cts!.Token);
                    StartReader(infoBaseInfo, position);
                }
            }
            catch (OperationCanceledException) {}
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обработки новых настроек экспорта журнала регистрации");
            }
        }
    }

    // Позволяет EventLogReductionService на время получить монопольный доступ к файлам журнала:
    // читатель снимается с сопровождения (не запуская OnReaderStopped/перезапуск) на время action,
    // а затем поднимается заново с позиции, подтверждённой репозиторием - так же, как при обычном
    // старте/перезапуске чтения.
    public async Task<bool> RunExclusiveAsync(string infoBaseId, Func<InfoBaseInfo, Task> action,
        CancellationToken cancellationToken)
    {
        BracketsEventLogReader? reader;

        lock (_readersLock)
        {
            reader = _readers.FirstOrDefault(c => c.InfoBaseInfo.InfoBaseId == infoBaseId);

            if (reader != null)
                _readers.Remove(reader);
        }

        if (reader is null)
            return false;

        var infoBaseInfo = reader.InfoBaseInfo;
        reader.Dispose();

        try
        {
            await action(infoBaseInfo);
        }
        finally
        {
            var cts = _cts;

            if (cts is { IsCancellationRequested: false })
            {
                // Пока action выполнялся, могли прийти новые настройки и уже поднять свежий
                // читатель для этой же ИБ (settingsQueue обработан параллельно с этим методом) -
                // тогда поднимать еще один не нужно, иначе получим два читателя одного файла.
                bool alreadyRestarted;
                lock (_readersLock)
                {
                    alreadyRestarted = _readers.Any(c => c.InfoBaseInfo.InfoBaseId == infoBaseInfo.InfoBaseId);
                }

                // ...а могли и выключить экспорт по этой ИБ - тогда воскрешать читателя для нее не нужно.
                var stillActive = _settings is { Enabled: true } &&
                    _settings.Items.Any(c => c.IsActive && c.InfoBase.InfoBaseInternalId == infoBaseInfo.InfoBaseId);

                if (!alreadyRestarted && stillActive)
                {
                    var position = await exporter.GetLastEventDateTime(infoBaseInfo.InfoBaseId, cancellationToken);
                    StartReader(infoBaseInfo, position);
                }
            }
        }

        return true;
    }

    private void StartReader(InfoBaseInfo infoBaseInfo, DateTime position)
    {
        var reader = new BracketsEventLogReader(infoBaseInfo, exporter, logReaderLogger);
        reader.Stopped += (_, _) => OnReaderStopped(reader, infoBaseInfo);

        lock (_readersLock)
        {
            _readers.Add(reader);
        }

        reader.Start(position, _cts!.Token);
    }

    // Ридер сигнализирует о непредвиденном падении через Stopped - без этого менеджер
    // узнал бы о мёртвом ридере только при следующей смене настроек
    private void OnReaderStopped(BracketsEventLogReader reader, InfoBaseInfo infoBaseInfo)
    {
        lock (_readersLock)
        {
            if (!_readers.Remove(reader))
                return; // ридер уже заменён более свежим циклом обработки настроек
        }

        reader.Dispose();

        var cts = _cts;
        if (cts is null || cts.IsCancellationRequested)
            return;

        _ = RestartReaderAsync(infoBaseInfo, cts.Token);
    }

    private async Task RestartReaderAsync(InfoBaseInfo infoBaseInfo, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogWarning("Перезапуск чтения журнала регистрации после непредвиденной ошибки - {Name}",
                infoBaseInfo.Name);

            var position = await exporter.GetLastEventDateTime(infoBaseInfo.InfoBaseId, cancellationToken);
            StartReader(infoBaseInfo, position);
        }
        catch (OperationCanceledException) {}
        catch (Exception e)
        {
            logger.LogError(e, "Не удалось перезапустить чтение журнала регистрации - {Name}", infoBaseInfo.Name);
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
        List<BracketsEventLogReader> readers;

        lock (_readersLock)
        {
            readers = [.._readers];
            _readers.Clear();
        }

        readers.ForEach(c => c.Dispose());
    }

    public override void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        DisposeReaders();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
