using System.Threading.Tasks.Dataflow;
using OnecMonitor.Agent.Extensions;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.EventLog;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;
using Timer = System.Timers.Timer;

namespace OnecMonitor.Agent.Services.EventLog;

public class EventLogExporter : IDisposable
{
    private IEventLogRepository? _repository;
    private readonly ActionBlock<EventLogItem[]>? _senderBlock;
    private readonly BatchBlock<EventLogItem>? _eventsBatchBlock;
    private readonly Timer _timer = new(5000);

    public EventLogExporter(IHostApplicationLifetime applicationLifetime)           
    {   
        _senderBlock = new ActionBlock<EventLogItem[]>(async batch => 
            await _repository!.WriteEvents(batch, applicationLifetime.ApplicationStopping));

        _eventsBatchBlock = new BatchBlock<EventLogItem>(5000, new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 5000 * 3,
            CancellationToken = applicationLifetime.ApplicationStopping
        });
        _eventsBatchBlock.LinkTo(_senderBlock, new DataflowLinkOptions { PropagateCompletion = true });
        
        _timer.Elapsed += (_, _) => _eventsBatchBlock!.TriggerBatch();
    }

    public async Task Init(EventLogSettingsDto settings, CancellationToken cancellationToken)
    {
        if (_repository != null)
        {
            _eventsBatchBlock!.TriggerBatch();
            _eventsBatchBlock!.Complete();
            await _senderBlock!.Completion;
            
            _repository?.Dispose();
        }

        if (settings.Enabled)
        {
            if (settings.Dbms.Type != DbmsType.ClickHouse)
                throw new Exception("DbmsType must be ClickHouse");

            _repository = new ClickHouseContext(settings.Dbms, settings.Credentials, settings.DatabaseName, settings.Table);
            await _repository.Connect(cancellationToken);
        
            _timer.Start();
        }
        else
            _timer.Stop();
    }
    
    public void Send(EventLogItem eventLogItem)
    {
        if (_repository == null)
            throw new InvalidOperationException("EventLogExporter is not initialized");

        _timer.Reset();
        _eventsBatchBlock!.Post(eventLogItem);
    }

    public void Dispose()
    {
        _repository?.Dispose();
        _timer.Dispose();
    }
}