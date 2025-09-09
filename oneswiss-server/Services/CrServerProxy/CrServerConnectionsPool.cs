using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services.CrServerProxy;

public class CrServerConnectionsPool
{
    private readonly ConcurrentDictionary<string, CrServerConnection> _serversConnections = new();

    public List<CrServerConnection> ServersConnections => _serversConnections.Values.ToList();

    public CrServerConnection GetServerConnection(HttpContext context, ConfigurationRepository repository)
    {
        var clientConn = context.Connection;
        var key = $"{repository.Name}_{clientConn.RemoteIpAddress}_{clientConn.LocalPort}";
        
        if (_serversConnections.TryGetValue(key, out var connection))
            return connection;
        
        connection = new CrServerConnection(key, repository.Host, repository.Port);
        _serversConnections.TryAdd(key, connection);
        
        return connection;
    }

    public void RemoveServerConnection(CrServerConnection connection)
    {
        _serversConnections.TryRemove(connection.Key, out _);
    }
}