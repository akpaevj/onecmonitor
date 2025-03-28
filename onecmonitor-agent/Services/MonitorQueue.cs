using System.Threading.Channels;

namespace OnecMonitor.Agent.Services;

public class MonitorQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public async Task QueueAsync(T item, CancellationToken cancellationToken)
        => await _channel.Writer.WriteAsync(item, cancellationToken);
    
    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
}