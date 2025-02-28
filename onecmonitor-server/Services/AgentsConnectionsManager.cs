using OnecMonitor.Common.TechLog;
using OnecMonitor.Server.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.DTO;

namespace OnecMonitor.Server.Services
{
    public class AgentsConnectionsManager(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        TechLogProcessor techLogProcessor,
        ILogger<AgentsConnectionsManager> logger) : BackgroundService
    {
        private readonly string _host = configuration.GetValue("OnecMonitor:Tcp:Host", "0.0.0.0");
        private readonly int _port = configuration.GetValue("OnecMonitor:Tcp:Port", 7001);
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        private readonly HashSet<AgentConnection> _connections = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Parse(_host), _port));

            logger.LogInformation($"Прослушивание агентов: {_host}:{_port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _socket.Listen();

                var client = await _socket.AcceptAsync(stoppingToken);

                var agentConnection = new AgentConnection(client, techLogProcessor, serviceProvider);
                agentConnection.AgentConnected += AgentConnection_Connected;
                agentConnection.AgentDisconnected += AgentConnection_Disconnected;

                agentConnection.Listen(stoppingToken);
            }

            _socket.Close();
        }

        private void AgentConnection_Connected(AgentConnection agentConnection)
        {
            lock (_connections)
                _connections.Add(agentConnection);
            
            logger.LogInformation($"Агент подключился: {agentConnection.AgentInstance!.InstanceName}. Идентификатор соединения: {agentConnection.ConnectionId}");
        }

        private void AgentConnection_Disconnected(AgentConnection agentConnection)
        {
            lock (_connections)
                _connections.Remove(agentConnection);

            agentConnection.AgentConnected -= AgentConnection_Connected;
            agentConnection.AgentDisconnected -= AgentConnection_Disconnected;

            logger.LogInformation($"Агент отключился: {agentConnection.AgentInstance!.InstanceName}. Идентификатор соединения: {agentConnection.ConnectionId}");
        }

        public bool IsConnected(Guid agentId)
            => GetAgentConnection(agentId) != null;

        public AgentConnection? GetAgentConnection(Guid agentId)
        {
            AgentConnection? agentConnection;
            
            lock (_connections)
                agentConnection = _connections.FirstOrDefault(c => c.AgentInstance!.MainConnection && c.AgentInstance.Id == agentId);

            return agentConnection;
        }
        
        public List<AgentConnection> GetAgentsConnections(List<Agent> agents)
            => agents.Select(c => GetAgentConnection(c.Id)).Where(c => c != null).ToList()!;
        
        public List<Agent> GetConnectedAgents(List<Agent> agents)
            => agents.Where(c => IsConnected(c.Id)).ToList();
    }
}
