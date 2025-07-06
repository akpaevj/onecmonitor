using System.Threading.Tasks.Dataflow;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Models;
using OneSwiss.Common.Services;
using OneSwiss.Common.TechLog;
using Timer = System.Timers.Timer;

namespace OneSwiss.Agent.Services.TechLog;

public class TechLogExporter
{
    private readonly TechLogRepositoryManager _repositoryManager;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<TechLogExporter> _logger;

    private ITechLogRepository? _repository;
    private CancellationTokenSource? _cts;
    private Timer? _flushTimer;

    private ActionBlock<TjEvent[]>? _sendBlock;
    private BatchBlock<TjEvent>? _batchBlock;
    private ActionBlock<TechLogEventContent>? _parseBlock;

    public TechLogExporter(
        TechLogRepositoryManager repositoryManager,
        IHostApplicationLifetime applicationLifetime, 
        ILogger<TechLogExporter> logger)
    {
        _repositoryManager = repositoryManager;
        _repositoryManager.SettingsChanged += SettingsChanged;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    private void SettingsChanged(object? sender, TechLogSettingsDto e)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.ApplicationStopping);

        if (e.Enabled)
            Init(_cts.Token);
    }

    private void Init(CancellationToken cancellationToken)
    {
        _repository = _repositoryManager.GetInstance();
        _repository.Connect(cancellationToken);
        
        var sendBlockOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = 10000
        };

        _sendBlock = new ActionBlock<TjEvent[]>(async tjEvents =>
        {
            try
            {
                await _repository.WriteEvents(tjEvents, cancellationToken);

                _logger.LogTrace("Tj events batch has been sent to the database");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to send tech log events batch to the database");
            }
        }, sendBlockOptions);

        var batchBlockOptions = new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 10000
        };
        _batchBlock = new BatchBlock<TjEvent>(5000, batchBlockOptions);

        var parseBlockOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BoundedCapacity = 10000,
        };
        _parseBlock = new ActionBlock<TechLogEventContent>(async i =>
        {
            try
            {
                if (TechLogParser.TryParse(i, out var tjEvent))
                    await _batchBlock.SendAsync(tjEvent, cancellationToken);
                else
                    _logger.LogError($"Ошибка разбора события технологического журнала: {i.Content}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка разбора события технологического журнала: {i.Content}");
            }
        }, parseBlockOptions);
        
        cancellationToken.Register(_parseBlock.Complete);

        _ = _parseBlock.Completion.ContinueWith(_ => _batchBlock.Complete(), cancellationToken);
        _batchBlock.LinkTo(_sendBlock!, new DataflowLinkOptions { PropagateCompletion = true });

        _flushTimer = new Timer(1000);
        _flushTimer.Elapsed += (_, _) => _batchBlock.TriggerBatch();
        _flushTimer.Start();
    }

    public async Task ProcessTjEventContent(TechLogEventContent eventContent, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Отправка события технологического журнала в блок разбора");
        await _parseBlock!.SendAsync(eventContent, cancellationToken);
    }
}