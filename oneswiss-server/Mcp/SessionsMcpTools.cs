using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Server.Mcp;

[McpServerToolType]
public sealed class SessionsMcpTools(
    AppDbContext dbContext,
    AgentsConnectionsManager agentsConnectionsManager,
    IConfiguration configuration)
{
    private static readonly string[] AllowedMetrics =
    [
        "cpu_current", "cpu_total", "duration_current", "duration_current_dbms", "memory_current", "calls_last_5min"
    ];

    [McpServerTool(Name = "sessions_analyze_load", ReadOnly = true)]
    [Description("Анализирует активные сеансы 1С и определяет, какие из них создают наибольшую нагрузку на " +
                  "сервер. Без clusterId опрашивает все известные кластеры (ровно одно обращение к RAS на " +
                  "кластер - без дорогого поинфобазного обхода). Без параметра metric возвращает четыре среза: " +
                  "по CPU, по времени работы с СУБД, по памяти и по блокировкам - каждая сессия несёт полный " +
                  "набор метрик независимо от того, по какому срезу она туда попала. С параметром metric " +
                  "возвращает один топ по указанному показателю. Если в настройках сервера включена " +
                  "анонимизация (Mcp:AnonymizeSessionData, по умолчанию выключена), чувствительные поля " +
                  "(логин, имя компьютера, разделитель данных) заменяются псевдонимами (user-1, host-1, " +
                  "tenant-1), стабильными только в рамках этого одного ответа. IP-адрес клиента и текст " +
                  "текущего запроса к СУБД не возвращаются вовсе независимо от этой настройки.")]
    [Authorize(Roles = $"{Roles.ReadSessions},{Roles.CloseSessions}")]
    public async Task<SessionLoadAnalysisResult> AnalyzeLoad(
        [Description("Идентификатор кластера. Если не указан, анализируются все известные кластеры.")]
        Guid? clusterId = null,
        [Description("Идентификатор информационной базы (учитывается только вместе с clusterId) - " +
                      "ограничить анализ одной ИБ.")]
        Guid? infoBaseId = null,
        [Description("Метрика для единого топа вместо четырёх срезов: cpu_current, cpu_total, " +
                      "duration_current, duration_current_dbms, memory_current, calls_last_5min.")]
        string? metric = null,
        [Description("Сколько сессий возвращать в каждом топе/срезе (по умолчанию 10, максимум 50).")]
        int top = 10,
        [Description("Включать ли \"спящие\" (hibernate) сессии - по умолчанию исключаются, т.к. они " +
                      "не создают нагрузку прямо сейчас.")]
        bool includeHibernated = false,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(metric) && !AllowedMetrics.Contains(metric))
            throw new McpException($"Неизвестная метрика \"{metric}\". Допустимые значения: {string.Join(", ", AllowedMetrics)}");

        if (infoBaseId.HasValue && !clusterId.HasValue)
            throw new McpException("infoBaseId указывается только вместе с clusterId");

        var clampedTop = Math.Clamp(top, 1, 50);
        var clusters = await ResolveClustersAsync(clusterId, cancellationToken);

        var fetchResults = await Task.WhenAll(
            clusters.Select(cluster => FetchClusterSessionsAsync(cluster, infoBaseId, cancellationToken)));

        var sessions = new List<(Cluster Cluster, V8Session Session)>();
        var clustersScanned = new List<string>();
        var clustersSkipped = new List<ClusterSkipReason>();

        foreach (var result in fetchResults)
        {
            if (result.SkipReason != null)
            {
                clustersSkipped.Add(new ClusterSkipReason(result.Cluster.Name, result.SkipReason));
                continue;
            }

            clustersScanned.Add(result.Cluster.Name);
            sessions.AddRange(result.Sessions!.Select(s => (result.Cluster, s)));
        }

        if (!includeHibernated)
            sessions = sessions.Where(x => !x.Session.Hibernate).ToList();

        var anonymize = configuration.GetValue("Mcp:AnonymizeSessionData", false);
        var userLabels = new Dictionary<string, string>();
        var hostLabels = new Dictionary<string, string>();
        var tenantLabels = new Dictionary<string, string>();

        var items = sessions
            .Select(x => ToLoadItem(x.Cluster, x.Session, anonymize, userLabels, hostLabels, tenantLabels))
            .ToList();

        if (!string.IsNullOrEmpty(metric))
        {
            var top1 = RankBy(items, metric).Take(clampedTop).ToList();
            return new SessionLoadAnalysisResult(clustersScanned, clustersSkipped, items.Count, metric, top1, null, null, null, null);
        }

        var byCpu = items.OrderByDescending(i => i.CpuTimeCurrent).Take(clampedTop).ToList();
        var byDbms = items.OrderByDescending(i => i.DurationCurrentDbms).Take(clampedTop).ToList();
        var byMemory = items.OrderByDescending(i => i.MemoryCurrent).Take(clampedTop).ToList();
        var byLocks = items.OrderByDescending(i => i.BlockedByDbms + i.BlockedByLs).Take(clampedTop).ToList();

        return new SessionLoadAnalysisResult(clustersScanned, clustersSkipped, items.Count, null, null, byCpu, byDbms, byMemory, byLocks);
    }

    private async Task<List<Cluster>> ResolveClustersAsync(Guid? clusterId, CancellationToken cancellationToken)
    {
        if (clusterId.HasValue)
        {
            var cluster = await dbContext.Clusters
                .AsNoTracking()
                .Include(c => c.Credentials)
                .SingleOrDefaultAsync(c => c.Id == clusterId.Value, cancellationToken);

            if (cluster == null)
                throw new McpException("Кластер не найден");

            return [cluster];
        }

        return await dbContext.Clusters.AsNoTracking().Include(c => c.Credentials).ToListAsync(cancellationToken);
    }

    private async Task<ClusterSessionsFetchResult> FetchClusterSessionsAsync(
        Cluster cluster, Guid? infoBaseId, CancellationToken cancellationToken)
    {
        var connection = agentsConnectionsManager.GetAgentConnection(cluster.AgentId);
        if (connection == null)
            return new ClusterSessionsFetchResult(cluster, null, "Агент не подключен");

        try
        {
            List<V8Session> clusterSessions;

            if (infoBaseId.HasValue)
            {
                var infoBase = await dbContext.InfoBases
                    .AsNoTracking()
                    .Include(i => i.Credentials)
                    .SingleOrDefaultAsync(i => i.Id == infoBaseId.Value && i.ClusterId == cluster.Id, cancellationToken);

                if (infoBase == null)
                    throw new McpException("Информационная база не найдена для указанного кластера");

                clusterSessions = await connection.GetV8Sessions(cluster, infoBase, cancellationToken);
            }
            else
            {
                clusterSessions = await connection.GetV8Sessions(cluster, null, cancellationToken);
            }

            return new ClusterSessionsFetchResult(cluster, clusterSessions, null);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception e)
        {
            return new ClusterSessionsFetchResult(cluster, null, e.Message);
        }
    }

    private static IOrderedEnumerable<SessionLoadItem> RankBy(List<SessionLoadItem> items, string metric)
    {
        return metric switch
        {
            "cpu_current" => items.OrderByDescending(i => i.CpuTimeCurrent),
            "cpu_total" => items.OrderByDescending(i => i.CpuTimeTotal),
            "duration_current" => items.OrderByDescending(i => i.DurationCurrent),
            "duration_current_dbms" => items.OrderByDescending(i => i.DurationCurrentDbms),
            "memory_current" => items.OrderByDescending(i => i.MemoryCurrent),
            "calls_last_5min" => items.OrderByDescending(i => i.CallsLast5Min),
            _ => items.OrderByDescending(i => i.CpuTimeCurrent)
        };
    }

    private static SessionLoadItem ToLoadItem(
        Cluster cluster,
        V8Session session,
        bool anonymize,
        Dictionary<string, string> userLabels,
        Dictionary<string, string> hostLabels,
        Dictionary<string, string> tenantLabels)
    {
        return new SessionLoadItem(
            cluster.Id,
            cluster.Name,
            session.InfoBase?.Name ?? string.Empty,
            session.SessionId,
            session.Id,
            anonymize ? Pseudonymize(userLabels, session.UserName, "user") : session.UserName ?? string.Empty,
            anonymize ? Pseudonymize(hostLabels, session.Host, "host") : session.Host ?? string.Empty,
            anonymize ? Pseudonymize(tenantLabels, session.DataSeparation, "tenant") : session.DataSeparation ?? string.Empty,
            session.AppId ?? string.Empty,
            session.StartedAt,
            session.LastActiveAt,
            session.Hibernate,
            session.CpuTimeCurrent,
            session.CpuTimeLast5Min,
            session.CpuTimeTotal,
            session.MemoryCurrent,
            session.MemoryLast5Min,
            session.MemoryTotal,
            session.DurationCurrent,
            session.DurationLast5Min,
            session.DurationAll,
            session.DurationCurrentDbms,
            session.DurationLast5MinDbms,
            session.DurationAllDbms,
            session.CallsLast5Min,
            session.CallsAll,
            session.BlockedByDbms,
            session.BlockedByLs);
    }

    private static string Pseudonymize(Dictionary<string, string> labels, string? rawValue, string prefix)
    {
        if (string.IsNullOrEmpty(rawValue))
            return string.Empty;

        if (!labels.TryGetValue(rawValue, out var label))
        {
            label = $"{prefix}-{labels.Count + 1}";
            labels[rawValue] = label;
        }

        return label;
    }

    private sealed record ClusterSessionsFetchResult(Cluster Cluster, List<V8Session>? Sessions, string? SkipReason);
}

public sealed record ClusterSkipReason(string ClusterName, string Reason);

public sealed record SessionLoadItem(
    Guid ClusterId,
    string ClusterName,
    string InfoBaseName,
    string SessionNumber,
    string SessionRacId,
    string User,
    string Host,
    string DataSeparation,
    string AppId,
    DateTime StartedAt,
    DateTime LastActiveAt,
    bool Hibernate,
    long CpuTimeCurrent,
    long CpuTimeLast5Min,
    long CpuTimeTotal,
    long MemoryCurrent,
    long MemoryLast5Min,
    long MemoryTotal,
    long DurationCurrent,
    long DurationLast5Min,
    long DurationAll,
    long DurationCurrentDbms,
    long DurationLast5MinDbms,
    long DurationAllDbms,
    long CallsLast5Min,
    long CallsAll,
    long BlockedByDbms,
    long BlockedByLs);

public sealed record SessionLoadAnalysisResult(
    IReadOnlyList<string> ClustersScanned,
    IReadOnlyList<ClusterSkipReason> ClustersSkipped,
    int TotalSessionsConsidered,
    string? Metric,
    IReadOnlyList<SessionLoadItem>? Top,
    IReadOnlyList<SessionLoadItem>? ByCpuTime,
    IReadOnlyList<SessionLoadItem>? ByDbmsWait,
    IReadOnlyList<SessionLoadItem>? ByMemory,
    IReadOnlyList<SessionLoadItem>? ByLocks);
