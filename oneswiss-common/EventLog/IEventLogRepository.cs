namespace OneSwiss.Common.EventLog;

public interface IEventLogRepository : IDisposable
{
    Task Connect(CancellationToken cancellationToken);
    Task InitDatabase(CancellationToken cancellationToken);
    Task InitEventLogTable(CancellationToken cancellationToken);
    Task<DateTime> GetLastEventDateTime(string infoBaseId, CancellationToken cancellationToken);
    Task WriteEvents(EventLogItem[] events, CancellationToken cancellationToken);
    Task<List<EventLogItem>> GetEventLogItems(string filter = "", CancellationToken cancellationToken = default);
    Task<List<EventLogItem>> GetEventLogItems(int count, int offset, string filter = "",
        CancellationToken cancellationToken = default);
    Task<int> GetRowsCount(string filter = "", CancellationToken cancellationToken = default);
    Task<List<string>> GetEventsTypes(string filter = "", CancellationToken cancellationToken = default);
}