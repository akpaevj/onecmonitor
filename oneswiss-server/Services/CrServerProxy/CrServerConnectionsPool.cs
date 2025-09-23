using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services.CrServerProxy;

public class CrServerConnectionsPool(ILogger<CrServerConnectionsPool> logger)
{
    private readonly ConcurrentDictionary<string, CrServerConnection> _serversConnections = new();

    public List<CrServerConnection> ServersConnections => _serversConnections.Values.ToList();

    public CrServerConnection GetServerConnection(HttpContext context, ConfigurationRepository repository)
    {
        var clientConn = context.Connection;
        var key = $"{repository.Name}_{clientConn.RemoteIpAddress}_{clientConn.LocalPort}";
        
        logger.LogTrace("Получение соединения к серверу хранилищ. Ключ - {Key}", key);
        if (_serversConnections.TryGetValue(key, out var connection))
            return connection;
        
        logger.LogTrace("Создание соединения к серверу хранилищ. Ключ - {Key}", key);
        connection = new CrServerConnection(key, repository.Host, repository.Port);
        _serversConnections.TryAdd(key, connection);
        
        return connection;
    }

    public void RemoveServerConnection(CrServerConnection connection)
    {
        logger.LogTrace("Удаление соединения к серверу хранилищ из пула. Ключ - {Key}", connection.Key);
        _serversConnections.TryRemove(connection.Key, out _);
    }
}