using System.Data;
using Microsoft.Data.SqlClient;
using OneSwiss.Common.DTO;
using OneSwiss.V8.Platform.RemoteAdministration;

namespace OneSwiss.Agent.Helpers;

public record BackupInfo(
    string DatabaseName,
    DateTime BackupStartDate,
    DateTime BackupFinishDate,
    string BackupType,
    decimal BackupSizeMB,
    string PhysicalDeviceName);

public static class SqlHelper
{
    public static async Task<BackupInfo?> GetLastBackupInfo(
        V8InfoBaseDetails infoBaseDetails, 
        CredentialsDto credentials,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(GetConnectionString(infoBaseDetails, credentials));
        await connection.OpenAsync(cancellationToken);

        const string query = 
                         """
                         SELECT TOP 1
                             b.database_name AS DatabaseName,
                             b.backup_start_date AS BackupStartDate,
                             b.backup_finish_date AS BackupFinishDate,
                             CASE b.type 
                                 WHEN 'D' THEN 'Full' 
                                 WHEN 'I' THEN 'Differential' 
                                 WHEN 'L' THEN 'Log' 
                             END AS BackupType,
                             b.backup_size / 1024 / 1024 AS BackupSizeMB,
                             mf.physical_device_name AS PhysicalDeviceName
                         FROM msdb.dbo.backupset b
                         INNER JOIN msdb.dbo.backupmediafamily mf ON b.media_set_id = mf.media_set_id
                         WHERE b.type = 'D'
                             AND b.database_name = @DatabaseName
                         ORDER BY b.backup_finish_date DESC
                         """;

        await using var command = new SqlCommand(query, connection);
    
        command.Parameters.Add(new SqlParameter("@DatabaseName", SqlDbType.NVarChar, 128) 
        { 
            Value = infoBaseDetails.DbName 
        });

        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SingleRow, 
            cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new BackupInfo(
                DatabaseName: reader.GetString("DatabaseName"),
                BackupStartDate: reader.GetDateTime("BackupStartDate"),
                BackupFinishDate: reader.GetDateTime("BackupFinishDate"),
                BackupType: reader.GetString("BackupType"),
                BackupSizeMB: reader.GetDecimal("BackupSizeMB"),
                PhysicalDeviceName: reader.GetString("PhysicalDeviceName"));
        }

        return null;
    }

    private static string GetConnectionString(V8InfoBaseDetails infoBaseDetails, CredentialsDto credentials)
        => $"Server={infoBaseDetails.DbServer};Database={infoBaseDetails.DbName};User Id={credentials.User};Password={credentials.Password};";
}