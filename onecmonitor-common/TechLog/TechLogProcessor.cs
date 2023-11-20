using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using Exception = System.Exception;

namespace OnecMonitor.Common.TechLog
{
    public class TechLogProcessor : BackgroundService
    {
        private readonly ILogger<TechLogProcessor> _logger;

        private readonly ITechLogStorage _techLogStorage;
        private readonly ActionBlock<(AgentInstance, TechLogEventContentDto)> _parseblock;
        private readonly BatchBlock<TjEvent> _batchBlock;
        private readonly ActionBlock<TjEvent[]> _sendBlock;

        public TechLogProcessor(ITechLogStorage techLogStorage, ILogger<TechLogProcessor> logger)
        {
            _techLogStorage = techLogStorage;

            _logger = logger;

            var sendBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 10000
            };

            _sendBlock = new ActionBlock<TjEvent[]>(async tjEvents =>
            {
                try
                {
                    await _techLogStorage.AddTjEvents(tjEvents);

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
                try
                {
                    if (TechLogParser.TryParse(i.AgentInstance, i.Item, out var tjEvent))
                        await _batchBlock.SendAsync(tjEvent);
                    else
                        _logger.LogError($"Failed to parse tj event content: {i.Item.Content}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to parse tj event content: {i.Item.Content}");
                }
            }, parseBlockOptions);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(_parseblock.Complete);

            _ = _parseblock.Completion.ContinueWith(c => _batchBlock.Complete(), stoppingToken);
            _batchBlock.LinkTo(_sendBlock, new DataflowLinkOptions() { PropagateCompletion = true });

            while (!stoppingToken.IsCancellationRequested) 
            {
                _batchBlock!.TriggerBatch();
                await Task.Delay(1000, stoppingToken);
            }
        }

        public async Task ProcessTjEventContent(AgentInstance agentInstance, TechLogEventContentDto tjEventContent, CancellationToken cancellationToken = default)
        {
            await _parseblock.SendAsync((agentInstance, tjEventContent), cancellationToken);

            _logger.LogTrace("Tj event content has been sent to the parsing block");
        }
    }
}