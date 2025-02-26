using System.Threading.Channels;
using OnecMonitor.Common.DTO;

namespace OnecMonitor.Agent.Services.InfoBases;

public class InfoBasesUpdateTasksQueue
{
    private readonly Channel<Message> _updateRequestsChannel = Channel.CreateUnbounded<Message>();

    public async Task QueueAsync(Message message, CancellationToken cancellationToken)
        => await _updateRequestsChannel.Writer.WriteAsync(message, cancellationToken);
    
    public async Task<Message> DequeueAsync(CancellationToken cancellationToken)
        => await _updateRequestsChannel.Reader.ReadAsync(cancellationToken);
}