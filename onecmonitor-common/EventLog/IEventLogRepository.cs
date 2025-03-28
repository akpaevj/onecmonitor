namespace OnecMonitor.Common.EventLog;

public interface IEventLogRepository : IDisposable
{
    Task Connect(CancellationToken cancellationToken);
    Task InitDatabase(CancellationToken cancellationToken);
    Task InitEventLogTable(CancellationToken cancellationToken);
    Task<DateTime> GetLastEventDateTime(string infoBaseName, CancellationToken cancellationToken);
    Task WriteEvents(EventLogItem[] events, CancellationToken cancellationToken);
}