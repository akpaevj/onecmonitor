using System.Threading.Channels;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.DTO.MaintenanceTasks;

namespace OnecMonitor.Agent.Services.MaintenanceTasks;

public class MaintenanceTaskExecutorQueue
{
    private readonly Channel<MaintenanceTaskDto> _updateRequestsChannel = Channel.CreateUnbounded<MaintenanceTaskDto>();

    public async Task QueueAsync(MaintenanceTaskDto task, CancellationToken cancellationToken)
        => await _updateRequestsChannel.Writer.WriteAsync(task, cancellationToken);
    
    public async Task<MaintenanceTaskDto> DequeueAsync(CancellationToken cancellationToken)
        => await _updateRequestsChannel.Reader.ReadAsync(cancellationToken);
}