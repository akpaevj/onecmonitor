using System.Threading.Tasks.Dataflow;
using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using OneSwiss.Common.Models;
using OneSwiss.V8.Platform.Brackets;
using Timer = System.Timers.Timer;

namespace OneSwiss.Agent.Services.EventLog;

public class EventLogExporter : IAsyncDisposable
{
    private BatchBlock<EventLogItem>? _eventsBatchBlock;
    private ActionBlock<EventLogItem[]>? _senderBlock;
    private readonly Timer _timer;
    private IEventLogRepository? _repository;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private bool _disposed;

    private readonly ILogger<EventLogExporter> _logger;

    public EventLogExporter(IHostApplicationLifetime applicationLifetime, ILogger<EventLogExporter> logger)
    {
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _timer = new Timer(5000)
        {
            AutoReset = true
        };
        _timer.Elapsed += (_, _) =>
        {
            try
            {
                _eventsBatchBlock?.TriggerBatch();
            }
            catch
            {
                // пайплайн мог быть остановлен/пересоздан одновременно со срабатыванием таймера
            }
        };
    }

    public async Task Init(IEventLogRepository repository, EventLogSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Останавливаем текущий пайплайн и отправляем оставшиеся данные
        await CleanupCurrentPipelineAsync();

        if (settings.Enabled)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            await _repository.Connect(cancellationToken);

            _senderBlock = new ActionBlock<EventLogItem[]>(async batch =>
            {
                if (_repository is null) return;
                await _repository.WriteEvents(batch, _applicationLifetime.ApplicationStopping);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = _applicationLifetime.ApplicationStopping,
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 2
            });

            _eventsBatchBlock = new BatchBlock<EventLogItem>(5000, new GroupingDataflowBlockOptions
            {
                BoundedCapacity = 5000 * 3,
                CancellationToken = _applicationLifetime.ApplicationStopping
            });

            _eventsBatchBlock.LinkTo(_senderBlock, new DataflowLinkOptions { PropagateCompletion = true });

            // Fault ActionBlock'а не распространяется вверх по потоку сам по себе -
            // без этого наблюдателя продюсеры зависнут в SendAsync навсегда при ошибке записи
            _ = ObserveSenderCompletion(_senderBlock, _eventsBatchBlock);

            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _repository = null;
        }
    }

    private async Task ObserveSenderCompletion(ActionBlock<EventLogItem[]> senderBlock, BatchBlock<EventLogItem> batchBlock)
    {
        try
        {
            await senderBlock.Completion;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Пайплайн экспорта журнала регистрации остановлен из-за ошибки записи");
            ((IDataflowBlock)batchBlock).Fault(e);
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
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), _applicationLifetime.ApplicationStopping);
                    
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

    public async Task SendAsync(EventLogItem eventLogItem, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_eventsBatchBlock is null)
            throw new InvalidOperationException("EventLogExporter не инициализирован");

        // При переполнении блока накопления ждем освобождения места вместо немедленного отказа
        if (!await _eventsBatchBlock.SendAsync(eventLogItem, cancellationToken))
            throw new InvalidOperationException("Ошибка отправки события. Блок накопления завершен");
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
        }
    }

    // Реализация IDisposable для совместимости
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}