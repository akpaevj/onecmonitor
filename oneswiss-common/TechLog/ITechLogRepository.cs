using OneSwiss.Common.Models;

namespace OneSwiss.Common.TechLog;

public interface ITechLogRepository : IDisposable
{
    Task Connect(CancellationToken cancellationToken);
    Task InitDatabase(CancellationToken cancellationToken);
    Task InitTechLogTable(CancellationToken cancellationToken);

    Task<long> GetLastTechLogPosition(string agentId, string seanceId, string templateId, string fileName,
        CancellationToken cancellationToken);

    Task WriteEvents(TjEvent[] events, CancellationToken cancellationToken);
    Task<TjEvent?> GetTjEvent(string filter, CancellationToken cancellationToken = default);
    Task<TjEvent?> GetTjEvent(string filter, string[] fields, CancellationToken cancellationToken = default);

    Task<T?> GetTjEventProperties<T>(string filter, string[] fields, T anonTypeObject,
        CancellationToken cancellationToken = default);

    Task<List<TjEvent>> GetTjEvents(string filter = "", CancellationToken cancellationToken = default);

    Task<List<TjEvent>> GetTjEvents(int count, int offset, string filter = "",
        CancellationToken cancellationToken = default);

    Task<int> GetRowsCount(string filter = "", CancellationToken cancellationToken = default);
    Task DeleteTechLogSeanceData(string seanceId, CancellationToken cancellationToken = default);
    Task DeleteAgentData(string agentId, CancellationToken cancellationToken = default);
}