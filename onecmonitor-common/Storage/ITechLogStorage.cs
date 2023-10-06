using OnecMonitor.Server.Models;

namespace OnecMonitor.Common.Storage
{
    public interface ITechLogStorage
    {
        Task InitDatabase(CancellationToken cancellationToken = default);
        Task AddTjEvent(TjEvent item, CancellationToken cancellationToken = default);
        Task AddTjEvents(TjEvent[] items, CancellationToken cancellationToken = default);
        Task<TjEvent?> GetTjEvent(string filter, CancellationToken cancellationToken = default);
        Task<TjEvent?> GetTjEvent(string filter, string[] fields, CancellationToken cancellationToken = default);
        Task<T?> GetTjEventProperties<T>(string filter, string[] fields, T anonTypeObject, CancellationToken cancellationToken = default);
        Task<List<TjEvent>> GetTjEvents(string filter = "", CancellationToken cancellationToken = default);
        Task<List<TjEvent>> GetTjEvents(int count, int offset, string filter = "", CancellationToken cancellationToken = default);
        Task<int> GetTjEventsCount(string filter = "", CancellationToken cancellationToken = default);
        Task<long> GetLastFilePosition(string agentId, string seanceId, string templateId, string folder, string file, CancellationToken cancellationToken = default);
        Task DeleteTechLogSeanceData(string seanceId, CancellationToken cancellationToken = default);
    }
}