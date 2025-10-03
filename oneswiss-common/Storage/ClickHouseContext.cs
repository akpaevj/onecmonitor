using System.Text;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Dapper;
using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using OneSwiss.Common.Helpers;
using OneSwiss.Common.Models;
using OneSwiss.Common.TechLog;

namespace OneSwiss.Common.Storage;

public class ClickHouseContext(
    DbmsDto dbms,
    CredentialsDto credentials,
    string database,
    string table) : IEventLogRepository, ITechLogRepository
{
    private readonly string _tablePath = $"{database}.{table}";
    private ClickHouseConnection? _connection;

    public async Task Connect(CancellationToken cancellationToken)
    {
        if (_connection == null)
        {
            _connection = new ClickHouseConnection(BuildConnectionString());
            await _connection.OpenAsync(cancellationToken);
        }
    }

    public async Task InitDatabase(CancellationToken cancellationToken)
    {
        if (_connection == null)
            await Connect(cancellationToken);

        await _connection!.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS {database}");
    }

    public async Task InitEventLogTable(CancellationToken cancellationToken)
    {
        if (_connection == null)
            await InitDatabase(cancellationToken);

        var createTableCmd =
            $"""
             CREATE TABLE IF NOT EXISTS {_tablePath}
             (
                 Id UUID,
                 TtlDate DateTime('UTC') Codec(Delta, LZ4),
                 InfoBaseId LowCardinality(String),
                 InfoBaseName LowCardinality(String),
                 Level LowCardinality(String),
                 Date DateTime('UTC') Codec(Delta, LZ4),
                 ApplicationName LowCardinality(String),
                 Event LowCardinality(String),
                 User LowCardinality(String),
                 UserName LowCardinality(String),
                 Computer LowCardinality(String),
                 Metadata LowCardinality(String),
                 MetadataPresentation LowCardinality(String),
                 Comment String Codec(ZSTD),
                 Data String Codec(ZSTD),
                 DataPresentation String Codec(ZSTD),
                 TransactionStatus LowCardinality(String),
                 TransactionId Int64 Codec(DoubleDelta, LZ4),
                 TransactionDateTime DateTime('UTC') Codec(Delta, LZ4),
                 Connection Int64 Codec(DoubleDelta, LZ4),
                 Session Int64 Codec(DoubleDelta, LZ4),
                 ServerName LowCardinality(String),
                 Port Int32 Codec(DoubleDelta, LZ4),
                 SyncPort Int32 Codec(DoubleDelta, LZ4),
                 SessionDataSeparation String Codec(ZSTD),
                 SessionDataSeparationPresentation String Codec(ZSTD),
             )
             engine = MergeTree()
             PARTITION BY (toYYYYMM(Date), InfoBaseId)
             ORDER BY Date
             TTL TtlDate DELETE
             """;

        await _connection!.ExecuteAsync(createTableCmd);
    }

    public async Task<DateTime> GetLastEventDateTime(string infoBaseId, CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        return await _connection!.QuerySingleAsync<DateTime>($"SELECT MAX(Date) FROM {_tablePath} WHERE InfoBaseId = '{infoBaseId}'");
    }

    public async Task WriteEvents(EventLogItem[] events, CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        IReadOnlyCollection<string> readOnlyCollection = [
            "Id",
            "TtlDate",
            "InfoBaseId",
            "InfoBaseName",
            "Level",
            "Date",
            "ApplicationName",
            "Event",
            "User",
            "UserName",
            "Computer",
            "Metadata",
            "MetadataPresentation",
            "Comment",
            "Data",
            "DataPresentation",
            "TransactionStatus",
            "TransactionId",
            "TransactionDateTime",
            "Connection",
            "Session",
            "ServerName",
            "Port",
            "SyncPort",
            "SessionDataSeparation",
            "SessionDataSeparationPresentation"
        ];
        using var bulk = new ClickHouseBulkCopy(_connection)
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BatchSize = events.Length,
            ColumnNames =
            readOnlyCollection,
            DestinationTableName = _tablePath
        };

        await bulk.InitAsync();

        var items = events.Select(c => new object[]
        {
            c.Id,
            c.TtlDate,
            c.InfoBaseId,
            c.InfoBaseName,
            c.Level,
            c.Date.ToUniversalTime(),
            c.ApplicationName,
            c.Event,
            c.User,
            c.UserName,
            c.Computer,
            c.Metadata,
            c.MetadataPresentation,
            c.Comment,
            c.Data,
            c.DataPresentation,
            c.TransactionStatus,
            c.TransactionID,
            c.TransactionDateTime,
            c.Connection,
            c.Session,
            c.ServerName,
            c.Port,
            c.SyncPort,
            c.SessionDataSeparation,
            c.SessionDataSeparationPresentation
        });

        await bulk.WriteToServerAsync(items, cancellationToken);
    }
    
    public async Task<List<string>> GetEventsTypes(string filter = "", CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT DISTINCT on (Event)
                Event
             FROM {_tablePath}
             """);

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        var result = await _connection!.QueryAsync<string>(queryText.ToString());

        return result.ToList();
    }
    
    public async Task<List<EventLogItem>> GetEventLogItems(string filter = "", CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT 
                 *
             FROM {_tablePath}
             """);

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        var result = await _connection!.QueryAsync<EventLogItem>(queryText.ToString());

        return result.ToList();
    }

    public async Task<List<EventLogItem>> GetEventLogItems(int count, int offset, string filter = "",
        CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT 
                 *
             FROM {_tablePath}
             """);

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        queryText.Append($" ORDER BY Date DESC LIMIT {count} OFFSET {offset}");

        var result = await _connection!.QueryAsync<EventLogItem>(queryText.ToString());

        return result.ToList();
    }

    public async Task InitTechLogTable(CancellationToken cancellationToken)
    {
        if (_connection == null)
            await InitDatabase(cancellationToken);

        var createTableCmd =
            $"""
             CREATE TABLE IF NOT EXISTS {_tablePath}
             (
                 Id UUID,
                 AgentId UUID,
                 SeanceId UUID,
                 TemplateId UUID,
                 FileName String,
                 EndPosition Int64,
                 StartDateTime DateTime64(6, 'UTC') Codec(Delta, LZ4),
                 DateTime DateTime64(6, 'UTC') Codec(Delta, LZ4),
                 Duration Int64 Codec(DoubleDelta, LZ4),
                 EventName LowCardinality(String),
                 Level Int8 Codec(DoubleDelta, LZ4),
                 SessionId String,
                 CallId String,
                 TClientId Int32 Codec(DoubleDelta, LZ4),
                 DstClientId Int32 Codec(DoubleDelta, LZ4),
                 Usr String,
                 TConnectId String,
                 TComputerName String,
                 PProcessName LowCardinality(String),
                 Locks Array(String),
                 WaitConnections Array(Int32),
                 Properties Map(String, String)
             )
             ENGINE = MergeTree
             PARTITION BY toYYYYMMDD(DateTime)
             ORDER BY (EndPosition, EventName)
             """;

        await _connection!.ExecuteAsync(createTableCmd);
    }

    public async Task<long> GetLastTechLogPosition(string agentId, string seanceId, string templateId, string fileName,
        CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        var query =
            $"""
             SELECT 
                 EndPosition 
             FROM {_tablePath}
             WHERE
                 AgentId = toUUID('{agentId}')
                 and SeanceId = toUUID('{seanceId}')
                 and TemplateId = toUUID('{templateId}')
                 and FileName = '{fileName}'
             ORDER BY
                 DateTime DESC
             LIMIT 1
             """;

        return await _connection!.QueryFirstOrDefaultAsync<long>(query);
    }

    public async Task WriteEvents(TjEvent[] events, CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        using var bulk = new ClickHouseBulkCopy(_connection)
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BatchSize = events.Length,
            ColumnNames =
            [
                "Id",
                "AgentId",
                "SeanceId",
                "TemplateId",
                "FileName",
                "EndPosition",
                "StartDateTime",
                "DateTime",
                "Duration",
                "EventName",
                "Level",
                "SessionId",
                "CallId",
                "TClientId",
                "DstClientId",
                "Usr",
                "TConnectId",
                "TComputerName",
                "PProcessName",
                "Locks",
                "WaitConnections",
                "Properties"
            ],
            DestinationTableName = _tablePath
        };

        await bulk.InitAsync();

        await bulk.WriteToServerAsync(events.Select(i => new object[]
        {
            i.Id,
            i.AgentId,
            i.SeanceId,
            i.TemplateId,
            i.FileName,
            i.EndPosition,
            i.StartDateTime,
            i.DateTime,
            i.Duration,
            i.EventName,
            i.Level,
            i.SessionId,
            i.CallId,
            i.TClientId,
            i.DstClientId,
            i.Usr,
            i.TConnectId,
            i.TComputerName,
            i.PProcessName,
            i.Locks,
            i.WaitConnections,
            i.Properties
        }), cancellationToken);
    }

    public Task<TjEvent?> GetTjEvent(string filter, CancellationToken cancellationToken = default)
    {
        return GetTjEvent(filter, ["*"], cancellationToken);
    }

    public async Task<TjEvent?> GetTjEvent(string filter, string[] fields,
        CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder("SELECT\n");

        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                queryText.AppendLine(", ");

            queryText.Append(fields[i]);
        }

        queryText.AppendLine();

        queryText.AppendLine($"FROM {_tablePath}");

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        queryText.Append("\nLIMIT 1");

        return await _connection!.QueryFirstAsync<TjEvent>(queryText.ToString());
    }

    public async Task<T?> GetTjEventProperties<T>(string filter, string[] fields, T anonTypeObject,
        CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder("SELECT\n");

        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                queryText.AppendLine(", ");

            queryText.Append(fields[i]);
        }

        queryText.AppendLine();

        queryText.AppendLine($"FROM {_tablePath}");

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        queryText.Append("\nLIMIT 1");

        return await _connection!.QueryFirstAsync<T>(queryText.ToString());
    }

    public async Task<List<TjEvent>> GetTjEvents(string filter = "", CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT 
                 *
             FROM {_tablePath}
             """);

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        var result = await _connection!.QueryAsync<TjEvent>(queryText.ToString());

        return result.ToList();
    }

    public async Task<List<TjEvent>> GetTjEvents(int count, int offset, string filter = "",
        CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT 
                 *
             FROM {_tablePath}
             """);

        if (!string.IsNullOrEmpty(filter))
        {
            queryText.Append("\nWHERE ");
            queryText.Append(filter);
        }

        queryText.Append($" ORDER BY DateTime DESC LIMIT {count} OFFSET {offset}");

        var result = await _connection!.QueryAsync<TjEvent>(queryText.ToString());

        return result.ToList();
    }

    public async Task<int> GetRowsCount(string filter = "", CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var queryText = new StringBuilder(
            $"""
             SELECT 
                 COUNT(*) 
             FROM {_tablePath}
             """);

        if (string.IsNullOrEmpty(filter))
            return await _connection!.QueryFirstAsync<int>(queryText.ToString());

        queryText.Append("\nWHERE ");
        queryText.Append(filter);

        return await _connection!.QueryFirstAsync<int>(queryText.ToString());
    }

    public async Task DeleteTechLogSeanceData(string seanceId, CancellationToken cancellationToken = default)
    {
        await Connect(cancellationToken);

        var query = $"ALTER TABLE {_tablePath} DELETE WHERE SeanceId = toUUID('{seanceId}')";

        await _connection!.ExecuteAsync(query);
    }

    public async Task<long> GetLastFilePosition(string agentId, string seanceId, string templateId, string fileName,
        CancellationToken cancellationToken)
    {
        await Connect(cancellationToken);

        var query =
            $"""
             SELECT 
                 EndPosition 
             FROM {_tablePath}
             PREWHERE
                 AgentId = toUUID('{agentId}')
                 and SeanceId = toUUID('{seanceId}')
                 and TemplateId = toUUID('{templateId}')
                 and FileName = '{fileName}'
             ORDER BY
                 DateTime DESC
             LIMIT 1
             """;

        return await _connection!.QueryFirstOrDefaultAsync<long>(query);
    }

    private string BuildConnectionString()
    {
        return $"Host={dbms.Host};Port={dbms.Port};Username={credentials.User};Password={credentials.Password}";
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}