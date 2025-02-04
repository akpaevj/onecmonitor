using OnecMonitor.Common.TechLog;
using OnecMonitor.Server.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace OnecMonitor.Server.Services
{
    public class AgentsConnectionsManager(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        TechLogProcessor techLogProcessor,
        ILogger<AgentsConnectionsManager> logger)
        : BackgroundService
    {
        private readonly string _host = configuration.GetValue("OnecMonitor:Tcp:Host", "0.0.0.0");
        private readonly int _port = configuration.GetValue("OnecMonitor:Tcp:Port", 7001);
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // k - agent id, v - connection id
        private readonly ConcurrentDictionary<Guid, Guid> _commandsSubscribers = new();

        private ConcurrentDictionary<Guid, AgentConnection> Connections { get; } = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Parse(_host), _port));

            logger.LogInformation($"Listening agents on: {_host}:{_port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _socket.Listen();

                var client = await _socket.AcceptAsync(stoppingToken);

                var agentConnection = new AgentConnection(client, techLogProcessor, serviceProvider);
                agentConnection.AgentConnected += AgentConnection_Connected;
                agentConnection.AgentDisconnected += AgentConnection_Disconnected;
                agentConnection.SubscribedForCommands += AgentConnection_SubscribedForCommands;

                _ = agentConnection.Listen(stoppingToken);
            }

            _socket.Close();
        }

        private void AgentConnection_Connected(AgentConnection agentConnection)
        {
            Connections.TryAdd(agentConnection.ConnectionId, agentConnection);

            logger.LogInformation($"Agent connected: {agentConnection.AgentInstance!.InstanceName}");
        }

        private void AgentConnection_Disconnected(AgentConnection agentConnection)
        {
            Connections.TryRemove(agentConnection.ConnectionId, out _);

            agentConnection.AgentConnected -= AgentConnection_Connected;
            agentConnection.AgentDisconnected -= AgentConnection_Disconnected;
            agentConnection.SubscribedForCommands -= AgentConnection_SubscribedForCommands;

            var commandsWatcher = _commandsSubscribers.FirstOrDefault(c => c.Value == agentConnection.ConnectionId);
            if (commandsWatcher.Key != Guid.Empty)
                _commandsSubscribers.TryRemove(commandsWatcher.Key, out _);

            logger.LogInformation($"Agent disconnected: {agentConnection.AgentInstance!.InstanceName}");
        }

        private void AgentConnection_SubscribedForCommands(AgentConnection agentConnection)
        {
            _commandsSubscribers.TryAdd(agentConnection.AgentInstance!.Id, agentConnection.ConnectionId);
        }

        public bool IsConnected(Guid agentId)
            => _commandsSubscribers.ContainsKey(agentId);

        public AgentConnection? GetCommandsSubscriberConnection(Guid id)
        {
            if (_commandsSubscribers.TryGetValue(id, out var connectionId) &&
                Connections.TryGetValue(connectionId, out var agentConnection)) 
                return agentConnection;

            return null;
        }
        
        public List<Agent> GetConnectedAgents(List<Agent> agents)
            => agents.Where(c => _commandsSubscribers.ContainsKey(c.Id)).ToList();

        public async Task UpdateTechLogSeances(List<Agent> agents, CancellationToken cancellationToken)
        {
            foreach(var agent in agents)
            {
                var connection = GetCommandsSubscriberConnection(agent.Id);
                if (connection == null)
                    continue;
                
                try
                {
                    await connection.UpdateTechLogSeances(null, cancellationToken);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
