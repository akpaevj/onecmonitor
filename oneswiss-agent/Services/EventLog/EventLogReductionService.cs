using OneSwiss.Common.DTO;
using OneSwiss.V8.Designer.Batch;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Services.EventLog;

// Периодически сокращает журнал регистрации источника до даты, уже подтверждённо экспортированной
// в БД oneswiss (EventLogExportManager/EventLogExporter). Работает отдельным сервисом, а не внутри
// EventLogExportManager, чтобы сбои/блокировки экспорта и обслуживания не были связаны напрямую.
public class EventLogReductionService(
    [FromKeyedServices(OneSwissConnection.CommonKey)]
    OneSwissConnection connection,
    EventLogSettingsState settingsState,
    EventLogExportManager exportManager,
    EventLogExporter exporter,
    V8ServicesProvider v8ServicesProvider,
    RasHolder rasHolder,
    ILogger<Rac> racLogger,
    ILogger<EventLogReductionService> logger)
    : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(15);

    private readonly Dictionary<Guid, DateTime> _lastAttemptDate = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        do
        {
            try
            {
                await Tick(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обработки свёртки журнала регистрации");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Tick(CancellationToken cancellationToken)
    {
        var settings = settingsState.Current;

        if (settings is not { Enabled: true, ReductionEnabled: true })
            return;

        if (DateTime.UtcNow.Hour != settings.ReductionHourUtc)
            return;

        foreach (var item in settings.Items.Where(c => c is { IsActive: true, ReduceSourceLog: true }))
        {
            if (_lastAttemptDate.TryGetValue(item.InfoBase.Id, out var lastAttempt) &&
                lastAttempt.Date == DateTime.UtcNow.Date)
                continue;

            try
            {
                // Троттлим раз в сутки только реальные попытки (успешная свёртка ниже, либо
                // исключение в catch) - дешевые ранние выходы из ProcessItem (нет данных, нет
                // прогресса, ридер сейчас недоступен) должны повторяться на следующем тике,
                // а не откладываться на сутки.
                if (await ProcessItem(item, settings, cancellationToken))
                    _lastAttemptDate[item.InfoBase.Id] = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _lastAttemptDate[item.InfoBase.Id] = DateTime.UtcNow;

                logger.LogError(e, "Ошибка свёртки журнала регистрации - {Name}", item.InfoBase.InfoBaseName);

                await connection.SendEventLogReductionResult(new EventLogReductionResultDto
                {
                    InfoBaseId = item.InfoBase.Id,
                    Success = false,
                    ErrorMessage = e.Message
                }, cancellationToken);
            }
        }
    }

    /// <returns>true, если была совершена содержательная попытка свёртки (успешная) - используется в Tick для троттлинга</returns>
    private async Task<bool> ProcessItem(EventLogExportItemDto item, EventLogSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var confirmedWatermark =
            await exporter.GetLastEventDateTime(item.InfoBase.InfoBaseInternalId, cancellationToken);

        if (confirmedWatermark == default)
            return false; // еще ни одного события не экспортировано - сокращать нечего

        var safeWatermark = confirmedWatermark - TimeSpan.FromHours(settings.ReductionSafetyMarginHours);
        var keepFloor = DateTime.UtcNow.AddDays(-item.ReduceKeepDays);
        var reduceDate = safeWatermark < keepFloor ? safeWatermark : keepFloor;

        if (item.LastReducedUpTo != null && reduceDate <= item.LastReducedUpTo)
            return false; // нет прогресса с прошлой свёртки

        var accessCode = Guid.NewGuid().ToString("N")[..8];

        var reduced = await exportManager.RunExclusiveAsync(item.InfoBase.InfoBaseInternalId, async infoBaseInfo =>
        {
            var ragent = v8ServicesProvider.GetActiveRagentByPort(item.InfoBase.Cluster.RagentPort);
            var ras = rasHolder.GetActiveRasForRagent(ragent);
            var rac = Rac.GetRacForRasService(racLogger, ras);

            var clusterId = item.InfoBase.Cluster.ClusterInternalId;
            var infoBaseId = item.InfoBase.InfoBaseInternalId;
            var clusterUser = item.InfoBase.Cluster.Credentials?.User ?? "";
            var clusterPassword = item.InfoBase.Cluster.Credentials?.Password ?? "";
            var ibUser = item.InfoBase.Credentials?.User ?? "";
            var ibPassword = item.InfoBase.Credentials?.Password ?? "";

            await rac.BlockConnections(clusterId, infoBaseId, accessCode,
                "Технические работы: свёртка журнала регистрации", clusterUser, clusterPassword, ibUser, ibPassword);

            try
            {
                var sessions = await rac.GetInfoBaseSessions(clusterId, infoBaseId, clusterUser, clusterPassword,
                    ibUser, ibPassword);

                foreach (var session in sessions.Where(c => !c.AppId.Contains("RAS", StringComparison.CurrentCultureIgnoreCase)))
                    try
                    {
                        await rac.TerminateSession(clusterId, session.Id, clusterUser, clusterPassword);
                    }
                    catch
                    {
                        // сеанс уже мог быть закрыт/повиснуть - не критично для дальнейшей свёртки
                    }

                using var batch = OnecV8BatchMode.CreateDesignerBatch(infoBaseInfo.Platform,
                    $"{item.InfoBase.Cluster.Host}:{item.InfoBase.Cluster.Port}", item.InfoBase.InfoBaseName);

                await batch.ReduceEventLogSize(reduceDate, ibUser, ibPassword, accessCode, waitForExit: true);
            }
            finally
            {
                await rac.UnblockConnections(clusterId, infoBaseId, clusterUser, clusterPassword, ibUser, ibPassword);
            }
        }, cancellationToken);

        if (!reduced)
            return false; // источник сейчас не читается этим агентом (например, ридер еще не поднялся) - попробуем в следующий раз

        item.LastReducedUpTo = reduceDate;

        await connection.SendEventLogReductionResult(new EventLogReductionResultDto
        {
            InfoBaseId = item.InfoBase.Id,
            Success = true,
            ReducedUpTo = reduceDate
        }, cancellationToken);

        return true;
    }
}
