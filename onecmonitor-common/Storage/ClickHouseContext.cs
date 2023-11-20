using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Query;
using Dapper;
using Newtonsoft.Json;
using OnecMonitor.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Common.Storage
{
    public class ClickHouseContext : IDisposable, ITechLogStorage
    {
        private const string RAW_TJEVENTS_TABLENAME = "raw_tjevents";

        private readonly string _database;
        private readonly ILogger<ClickHouseContext> _logger;
        private readonly ClickHouseConnection _connection;
        private bool disposedValue;

        public ClickHouseContext(ILogger<ClickHouseContext> logger, IConfiguration configuration)
        {
            _logger = logger;

            _database = configuration.GetValue("ClickHouse:Database", "default") ?? "default";

            var connectionStringBuilder = new ClickHouseConnectionStringBuilder
            {
                Host = configuration.GetValue("ClickHouse:Host", "localhost") ?? "localhost",
                Port = (ushort)configuration.GetValue("ClickHouse:Port", 8123),
                Username = configuration.GetValue("ClickHouse:User", "default") ?? "default",
                Password = configuration.GetValue("ClickHouse:Password", string.Empty) ?? string.Empty
            };

            var connectionString = connectionStringBuilder.ToString();

            _connection = new ClickHouseConnection(connectionString);
        }

        public async Task InitDatabase(CancellationToken cancellationToken = default)
        {
            try
            {
                await _connection.OpenAsync(cancellationToken);
                await _connection.ExecuteScalarAsync($"CREATE DATABASE IF NOT EXISTS {_database}");
                await _connection.ChangeDatabaseAsync(_database, cancellationToken);

                await _connection.ExecuteScalarAsync(
                    $"""
                    CREATE TABLE IF NOT EXISTS {RAW_TJEVENTS_TABLENAME}
                    (
                        Id UUID,
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
                        Properties Map(String, String),
                        AgentId UUID,
                        SeanceId UUID,
                        TemplateId UUID,
                        Folder String,
                        File String,
                        EndPosition Int64,
                        INDEX for_calls_chain(EventName,TClientId,CallId) TYPE minmax GRANULARITY 3
                    )
                    ENGINE = MergeTree
                    PARTITION BY (toYYYYMMDD(DateTime), EventName)
                    ORDER BY (EndPosition, EventName)
                    """);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, "Database init is canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to init the database");
            }
        }

        public async Task AddTjEvents(TjEvent[] items, CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            using var bulk = new ClickHouseBulkCopy(_connection)
            {
                DestinationTableName = RAW_TJEVENTS_TABLENAME,
                BatchSize = items.Length
            };

            try
            {
                await bulk.WriteToServerAsync(items.Select(i => new object[]
                {
                    i.Id,
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
                    i.Properties,
                    i.AgentId,
                    i.SeanceId,
                    i.TemplateId,
                    i.Folder,
                    i.File,
                    i.EndPosition
                }), cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, "Bulk load operation canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write data to the database");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _connection.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task<TjEvent?> GetTjEvent(string filter, CancellationToken cancellationToken = default)
            => GetTjEvent(filter, new string[] {"*"}, cancellationToken);

        public async Task<TjEvent?> GetTjEvent(string filter, string[] fields, CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var queryText = new StringBuilder("SELECT\n");

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    queryText.AppendLine(", ");

                queryText.Append(fields[i]);
            }

            queryText.AppendLine();

            queryText.AppendLine($"FROM {RAW_TJEVENTS_TABLENAME}");

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append("\nWHERE ");
                queryText.Append(filter);
            }

            queryText.Append("\nLIMIT 1");

            return await _connection.QueryFirstAsync<TjEvent>(queryText.ToString());
        }

        public async Task<T?> GetTjEventProperties<T>(string filter, string[] fields, T anonTypeObject, CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var queryText = new StringBuilder("SELECT\n");

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    queryText.AppendLine(", ");

                queryText.Append(fields[i]);
            }

            queryText.AppendLine();

            queryText.AppendLine($"FROM {RAW_TJEVENTS_TABLENAME}");

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append("\nWHERE ");
                queryText.Append(filter);
            }

            queryText.Append("\nLIMIT 1");

            return await _connection.QueryFirstAsync<T>(queryText.ToString());
        }

        public async Task<List<TjEvent>> GetTjEvents(string filter = "", CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var queryText = new StringBuilder(
                $@"SELECT 
                    *
                FROM {RAW_TJEVENTS_TABLENAME}");

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append("\nWHERE ");
                queryText.Append(filter);
            }

            var result = await _connection.QueryAsync<TjEvent>(queryText.ToString());

            return result.ToList();
        }

        public async Task<List<TjEvent>> GetTjEvents(int count, int offset, string filter = "", CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var queryText = new StringBuilder(
                $"""
                SELECT 
                    *
                FROM {RAW_TJEVENTS_TABLENAME}
                """);

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append("\nWHERE ");
                queryText.Append(filter);
            }

            queryText.Append($" ORDER BY DateTime DESC LIMIT {count} OFFSET {offset}");

            var result = await _connection.QueryAsync<TjEvent>(queryText.ToString());

            return result.ToList();
        }

        public async Task<int> GetTjEventsCount(string filter = "", CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var queryText = new StringBuilder(
                $"""
                SELECT 
                    COUNT(*) 
                FROM {RAW_TJEVENTS_TABLENAME}
                """);

            if (!string.IsNullOrEmpty(filter))
            {
                queryText.Append("\nWHERE ");
                queryText.Append(filter);
            }

            return await _connection.QueryFirstAsync<int>(queryText.ToString());
        }

        public async Task<long> GetLastFilePosition(string agentId, string seanceId, string templateId, string folder, string file, CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            try
            {
                var query =
                    $"""
                    SELECT 
                        EndPosition 
                    FROM {RAW_TJEVENTS_TABLENAME}
                    PREWHERE
                        AgentId = toUUID('{agentId}')
                        and SeanceId = toUUID('{seanceId}')
                        and TemplateId = toUUID('{templateId}')
                        and Folder = '{folder}'
                        and File = '{file}'
                    ORDER BY
                        DateTime DESC
                    LIMIT 1
                    """;

                return await _connection.QueryFirstOrDefaultAsync<long>(query);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last file position");
                throw;
            }
        }

        public async Task DeleteTechLogSeanceData(string seanceId, CancellationToken cancellationToken = default)
        {
            await OpenConnection(cancellationToken);

            var query = $"ALTER TABLE {RAW_TJEVENTS_TABLENAME} DELETE WHERE SeanceId = toUUID('{seanceId}')";

            await _connection.ExecuteAsync(query);
        }

        private async Task OpenConnection(CancellationToken cancellationToken = default)
        {
            await _connection.OpenAsync(cancellationToken);

            await _connection.ChangeDatabaseAsync(_database);
        }
    }
}
