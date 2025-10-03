using System.Threading.Tasks.Dataflow;
using ClickHouse.Client;
using OneSwiss.Agent.Extensions;
using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using Timer = System.Timers.Timer;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExporter : IAsyncDisposable
{
    private BatchBlock<EventLogItem>? _eventsBatchBlock;
    private ActionBlock<EventLogItem[]>? _senderBlock;
    private readonly Timer _timer;
    private IEventLogRepository? _repository;
    private readonly ILogger<EventLogExporter> _logger;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public EventLogExporter(ILogger<EventLogExporter> logger)
    {
        _logger = logger;

        _timer = new Timer(5000)
        {
            AutoReset = true
        };
        _timer.Elapsed += (_, _) => _eventsBatchBlock?.TriggerBatch();
    }

    public async Task Init(IEventLogRepository repository, EventLogSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
        
        _cts =  CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Останавливаем текущий пайплайн и отправляем оставшиеся данные
        await CleanupCurrentPipelineAsync();

        if (settings.Enabled)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            await _repository.Connect(_cts!.Token);

            _senderBlock = new ActionBlock<EventLogItem[]>(async batch =>
            {
                while (!_cts!.Token.IsCancellationRequested)
                {
                    if (_repository is null) 
                        break;

                    try
                    {
                        await _repository.WriteEvents(batch, _cts!.Token);
                        break;
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception e)
                    {
                        if (e is ClickHouseServerException { ErrorCode: 241 })
                            _logger.LogWarning(e, "Ошибка отправки данных в ClickHouse");
                        else
                            _logger.LogError(e, "Ошибка отправки данных в ClickHouse");
                        
                        await Task.Delay(60 * 1000, _cts!.Token);
                    }
                }
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = _cts!.Token,
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 2
            });

            _eventsBatchBlock = new BatchBlock<EventLogItem>(5000, new GroupingDataflowBlockOptions
            {
                BoundedCapacity = 5000 * 3,
                CancellationToken = _cts!.Token
            });

            _eventsBatchBlock.LinkTo(_senderBlock, new DataflowLinkOptions { PropagateCompletion = true });

            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _repository = null;
        }
    }

    private async Task CleanupCurrentPipelineAsync()
    {
        _timer.Stop();
        
        if (_eventsBatchBlock is not null)
        {
            try
            {
                _eventsBatchBlock.TriggerBatch();
                _eventsBatchBlock.Complete();
                
                if (_senderBlock is not null)
                {
                    var completionTask = _senderBlock.Completion;
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), _cts!.Token);
                    
                    await Task.WhenAny(completionTask, timeoutTask);
                }
            }
            catch
            {
                // ignore
            }
        }
        
        if (_repository is not null)
        {
            _repository.Dispose();
            _repository = null;
        }
        
        _eventsBatchBlock = null;
        _senderBlock = null;
    }

    public async Task Send(EventLogItem eventLogItem)
    {
        _timer.Reset();
        
        while (!_cts!.Token.IsCancellationRequested)
            if (_eventsBatchBlock!.Post(eventLogItem))
                break;
            else
                await Task.Delay(100, _cts!.Token);
    }

    public async Task<DateTime> GetLastEventDateTime(string infoBaseId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_repository is null)
            throw new InvalidOperationException("EventLogExporter не инициализирован");

        return await _repository.GetLastEventDateTime(infoBaseId, cancellationToken);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_eventsBatchBlock is not null)
        {
            _eventsBatchBlock.TriggerBatch();
            await Task.Delay(100, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        try
        {
            await CleanupCurrentPipelineAsync();
        }
        finally
        {
            _timer.Dispose();
            _repository?.Dispose();
            _cts?.Dispose();
        }
    }

    // Реализация IDisposable для совместимости
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}