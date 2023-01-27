using OnecMonitor.Common.Models;
using OnecMonitor.Server.Models;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using Exception = System.Exception;

namespace OnecMonitor.Server.Services
{
    public class TechLogProcessor : IDisposable
    {
        private readonly ILogger<TechLogProcessor> _logger;

        private readonly IClickHouseContext _clickHouseContext;
        private readonly AsyncServiceScope _scope;
        private readonly ActionBlock<(AgentInstance, TechLogEventContentDto)> _parseblock;
        private readonly BatchBlock<TjEvent> _batchBlock;
        private readonly ActionBlock<TjEvent[]> _sendBlock;
        private readonly Timer _batchTriggeringTimer;
        private bool disposedValue;

        public TechLogProcessor(IServiceProvider serviceProvider, ILogger<TechLogProcessor> logger)
        {
            _scope = serviceProvider.CreateAsyncScope();
            _clickHouseContext = _scope.ServiceProvider.GetRequiredService<IClickHouseContext>();

            _logger = logger;

            var sendBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 10000
            };

            _sendBlock = new ActionBlock<TjEvent[]>(async tjEvents =>
            {
                try
                {
                    await _clickHouseContext.AddTjEvents(tjEvents);

                    _logger.LogTrace("Tj events batch has been sent to the database");
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to send tech log events batch to the database");
                }
            }, sendBlockOptions);

            var batchBlockOptions = new GroupingDataflowBlockOptions()
            {
                BoundedCapacity = 10000
            };
            _batchBlock = new BatchBlock<TjEvent>(5000, batchBlockOptions);

            var parseBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 10000
            };
            _parseblock = new ActionBlock<(AgentInstance AgentInstance, TechLogEventContentDto Item)>(async i =>
            {
                if (TechLogParser.TryParse(i.AgentInstance, i.Item, out var tjEvent))
                    await _batchBlock.SendAsync(tjEvent);
                else
                    _logger.LogError($"Failed to parse tj event content: {i.Item.Content}");
            }, parseBlockOptions);

            _parseblock.Completion.ContinueWith(c => _batchBlock.Complete());
            _batchBlock.LinkTo(_sendBlock, new DataflowLinkOptions() { PropagateCompletion = true });

            _batchTriggeringTimer = new Timer(_ =>
            {
                _batchBlock!.TriggerBatch();
                _logger.LogTrace("Tech log batch block is triggered");
            }, null, 0, 1 * 1000);
        }

        public async Task ProcessTjEventContent(AgentInstance agentInstance, TechLogEventContentDto tjEventContent, CancellationToken cancellationToken = default)
        {
            await _parseblock.SendAsync((agentInstance, tjEventContent));

            _logger.LogTrace("Tj event content has been sent to the parsing block");
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _batchTriggeringTimer?.Dispose();
                    _parseblock?.Complete();
                    _scope.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}