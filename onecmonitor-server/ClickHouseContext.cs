using Clickhouse.Grpc;
using OnecMonitor.Server.Models;
using static Clickhouse.Grpc.ClickHouse;
using System.Text;
using Exception = System.Exception;
using Google.Protobuf;
using Newtonsoft.Json;
using Grpc.Core;

namespace OnecMonitor.Server
{
    public class ClickHouseContext : ClickHouseClient, IClickHouseContext
    {
        private readonly string _database;
        private readonly ILogger<ClickHouseContext> _logger;

        public string DatabaseName { get { return _database; } }

        public ClickHouseContext(ChannelBase channelBase, IConfiguration configuration, ILogger<ClickHouseContext> logger) : base(channelBase)
        {
            _logger = logger;
            _database = configuration.GetValue("ClickHouse:Database", "")!;
        }

        public ClickHouseContext(CallInvoker callInvoker, IConfiguration configuration, ILogger<ClickHouseContext> logger) : base(callInvoker)
        {
            _logger = logger;
            _database = configuration.GetValue("ClickHouse:Database", "")!;
        }

        public async Task InitDatabase(CancellationToken cancellationToken = default)
        {
            // Create database if it doesn't exist
            await ExecuteNonQuery($"CREATE DATABASE IF NOT EXISTS {_database}", cancellationToken);

            // Create raw tj events table
            // It's pretty difficult to avoid duplicated items, so let's try to smooth this case with ReplacingMergeTree engine,
            // agents might write duplicated items but during reading it will read only last version of each item
            await ExecuteNonQuery(
                $"""
                CREATE TABLE IF NOT EXISTS {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}
                (
                    id UUID,
                    start_date_time DateTime64(6, 'UTC') Codec(Delta, LZ4),
                    date_time DateTime64(6, 'UTC') Codec(Delta, LZ4),
                    duration Int64 Codec(DoubleDelta, LZ4),
                    event_name LowCardinality(String),
                    level Int8 Codec(DoubleDelta, LZ4),
                    session_id Int32 Codec(DoubleDelta, LZ4),
                    call_id Int32 Codec(DoubleDelta, LZ4),
                    t_client_id Int32 Codec(DoubleDelta, LZ4),
                    dst_client_id Int32 Codec(DoubleDelta, LZ4),
                    usr String,
                    t_connect_id Int32 Codec(DoubleDelta, LZ4),
                    t_computer_name String,
                    p_process_name LowCardinality(String),
                    regions Array(String),
                    locks Array(String),
                    wait_connections Array(Int32),
                    props Map(String, String),
                    _agent_id UUID,
                    _seance_id UUID,
                    _template_id UUID,
                    _folder String,
                    _file String,
                    _end_position Int64,
                    INDEX for_calls_chain(event_name,t_client_id,call_id) TYPE minmax GRANULARITY 3
                )
                ENGINE = MergeTree
                PARTITION BY (toYYYYMMDD(date_time), event_name)
                ORDER BY (_end_position, event_name)
                """
                , cancellationToken);
        }

        public async Task AddTjEvent(TjEvent item, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonConvert.SerializeObject(item);

                await ExecuteNonQuery($"INSERT INTO {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME} FORMAT JSONEachRow {json}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add new item");
                throw;
            }
        }

        public async Task AddTjEvents(TjEvent[] items, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonConvert.SerializeObject(items);

                await ExecuteNonQuery($"INSERT INTO {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME} FORMAT JSONEachRow {json}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add new items");
                throw;
            }
        }

        public async Task<TjEvent?> GetTjEvent(string filter, CancellationToken cancellationToken = default)
            => await GetTjEvent(filter, new string[] { "*" }, cancellationToken);

        public async Task<TjEvent?> GetTjEvent(string filter, string[] fields, CancellationToken cancellationToken = default)
        {
            var output = await GetTjEventOutput(filter, fields, cancellationToken);

            if (output.IsEmpty)
                return default;
            else
                return JsonConvert.DeserializeObject<TjEvent>(output.ToStringUtf8());
        }

        public async Task<T?> GetTjEventProperties<T>(string filter, string[] fields, T anonTypeObject, CancellationToken cancellationToken = default)
        {
            var output = await GetTjEventOutput(filter, fields, cancellationToken);

            if (output.IsEmpty)
                return default;
            else
                return JsonConvert.DeserializeAnonymousType(output.ToStringUtf8(), anonTypeObject);
        }

        public async Task<List<TjEvent>> GetTjEvents(string filter, CancellationToken cancellationToken = default)
        {
            var queryText = new StringBuilder(
                $@"SELECT 
                    *
                FROM {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}");

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append(" WHERE ");
                queryText.Append(filter);
            }

            var result = await ExecuteQueryAsync(new QueryInfo()
            {
                Query = queryText.ToString(),
                OutputFormat = "JSONEachRow"
            }, cancellationToken: cancellationToken);

            if (result.Exception != null)
                throw new Exception(result.Exception.DisplayText);

            if (result.Output.IsEmpty)
                return new();

            return DeserializeObjects<TjEvent>(result.Output);
        }

        public async Task<List<TjEvent>> GetTjEvents(int count, int offset, string filter = "", CancellationToken cancellationToken = default)
        {
            var queryText = new StringBuilder(
                $"""
                SELECT 
                    *
                FROM {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}
                """);

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append(" WHERE ");
                queryText.Append(filter);
            }

            queryText.Append($" ORDER BY date_time DESC LIMIT {count} OFFSET {offset}");

            try
            {
                var result = await ExecuteQueryAsync(new QueryInfo()
                {
                    Query = queryText.ToString(),
                    OutputFormat = "JSONEachRow"
                }, cancellationToken: cancellationToken);

                if (result.Exception != null)
                    throw new Exception(result.Exception.DisplayText);

                return DeserializeObjects<TjEvent>(result.Output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tj events");
                throw;
            }
        }

        public async Task<int> GetTjEventsCount(string filter = "", CancellationToken cancellationToken = default)
        {
            var queryText = new StringBuilder(
                $"""
                SELECT 
                    COUNT(*) 
                FROM {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}
                """);

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append(" WHERE ");
                queryText.Append(filter);
            }

            try
            {
                var result = await ExecuteQueryAsync(new QueryInfo()
                {
                    Query = queryText.ToString()
                }, cancellationToken: cancellationToken);

                if (result.Exception != null)
                    throw new Exception(result.Exception.DisplayText);

                return int.Parse(result.Output.ToStringUtf8());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tj events count");
                throw;
            }
        }

        public async Task<long> GetLastFilePosition(string agentId, string seanceId, string templateId, string folder, string file, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ExecuteQueryAsync(new QueryInfo()
                {
                    Query =
                    $"""
                    SELECT 
                        _end_position 
                    FROM {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}
                    PREWHERE
                        _agent_id = toUUID('{agentId}')
                        and _seance_id = toUUID('{seanceId}')
                        and _template_id = toUUID('{templateId}')
                        and _folder = '{folder}'
                        and _file = '{file}'
                    ORDER BY
                        date_time DESC
                    LIMIT 1
                    """
                }, cancellationToken: cancellationToken);

                if (result.Exception != null)
                    throw new Exception(result.Exception.DisplayText);

                if (result.Output.IsEmpty)
                    return 0;
                else
                    return long.Parse(result.Output.ToStringUtf8());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last file position");
                throw;
            }
        }

        public async Task DeleteTechLogSeanceData(string seanceId, CancellationToken cancellationToken = default)
        {
            var query = $"ALTER TABLE {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME} DELETE WHERE _seance_id = toUUID('{seanceId}')";

            await ExecuteNonQuery(query, cancellationToken);
        }

        private async Task<ByteString> GetTjEventOutput(string filter, string[] fields, CancellationToken cancellationToken = default)
        {
            var queryText = new StringBuilder("SELECT\n");

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    queryText.AppendLine(", ");

                queryText.Append(fields[i]);
            }

            queryText.AppendLine();

            queryText.AppendLine($"FROM {_database}.{IClickHouseContext.RAW_TJEVENTS_TABLENAME}");

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append(" WHERE ");
                queryText.Append(filter);
            }

            queryText.Append(" LIMIT 1");

            var result = await ExecuteQueryAsync(new QueryInfo()
            {
                Query = queryText.ToString(),
                OutputFormat = "JSONEachRow"
            }, cancellationToken: cancellationToken);

            if (result.Exception != null)
                throw new Exception(result.Exception.DisplayText);

            return result.Output;
        }
        
        private async Task ExecuteNonQuery(string query, CancellationToken cancellationToken = default)
        {
            var result = await ExecuteQueryAsync(new QueryInfo() { Query = query }, cancellationToken: cancellationToken);

            if (result.Exception != null)
                throw new Exception(result.Exception.DisplayText);
        }
        
        private static List<T> DeserializeObjects<T>(ByteString bytes)
            => DeserializeObjects<T>(bytes.ToArray());

        private static List<T> DeserializeObjects<T>(byte[] jsonBytes)
        {
            var result = new List<T>();

            using var mStream = new MemoryStream(jsonBytes);
            using var streamReader = new StreamReader(mStream);

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (line == null)
                    break;

                var item = JsonConvert.DeserializeObject<T>(line)!;
                result.Add(item);
            }

            return result;
        }
    }
}