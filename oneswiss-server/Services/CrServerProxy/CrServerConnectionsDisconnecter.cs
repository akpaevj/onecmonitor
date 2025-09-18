namespace OneSwiss.Server.Services.CrServerProxy;

public class CrServerConnectionsDisconnecter(CrServerConnectionsPool pool, ILogger<CrServerConnectionsDisconnecter> logger) : BackgroundService
{
    private const int TtlMinutes = 60;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var poolServersConnection in pool.ServersConnections.Where(poolServersConnection => !poolServersConnection.Blocked))
            {
                if (poolServersConnection is { Connected: false, Blocked: false })
                    CloseConnection(poolServersConnection, "соединение завершено сервером хранилищ");
                else if (poolServersConnection.LastUsingTimestamp != DateTime.MinValue &&
                         poolServersConnection.LastUsingTimestamp > DateTime.Now.AddMinutes(-TtlMinutes))
                    CloseConnection(poolServersConnection, "соединение завершено по истечение времени жизни");
            }

            await Task.Delay(100, stoppingToken);
        }
    }

    private void CloseConnection(CrServerConnection connection, string reason)
    {
        pool.RemoveServerConnection(connection);
        connection.Dispose();

        logger.LogTrace("Соединение с сервером хранилищ закрыто. Причина: {Reason}", reason);
    }
}