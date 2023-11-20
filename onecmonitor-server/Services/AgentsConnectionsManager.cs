using OnecMonitor.Common.Models;
using OnecMonitor.Common.TechLog;
using OnecMonitor.Server.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OnecMonitor.Server.Services
{
    public class AgentsConnectionsManager : BackgroundService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly Socket _socket;
        private readonly IServiceProvider _serviceProvider;
        private readonly TechLogProcessor _techLogProcessor;
        private readonly ILogger<AgentsConnectionsManager> _logger;

        // k - agent id, v - connection id
        private readonly ConcurrentDictionary<Guid, Guid> _commandsSubscribers = new();

        public ConcurrentDictionary<Guid, AgentConnection> Connections { get; private set; } = new();

        public AgentsConnectionsManager(
            IConfiguration configuration, 
            IServiceProvider serviceProvider,
            TechLogProcessor techLogProcessor,
            ILogger<AgentsConnectionsManager> logger) 
        {
            _serviceProvider = serviceProvider;
            _techLogProcessor = techLogProcessor;
            _logger = logger;

            _host = configuration.GetValue("OnecMonitor:Tcp:Host", "0.0.0.0")!;
            _port = configuration.GetValue("OnecMonitor:Tcp:Port", 7001);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Parse(_host), _port));

            _logger.LogInformation($"Listening agents on: {_host}:{_port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                _socket.Listen();

                var client = await _socket.AcceptAsync(stoppingToken);

                var agentConnection = new AgentConnection(client, _techLogProcessor, _serviceProvider);
                agentConnection.AgentConnected += AgentConnection_Connected;
                agentConnection.AgentDisconnected += AgentConnection_Disconnected;
                agentConnection.SubscribedForCommands += AgentConnection_SubscribedForCommands;

                _ = agentConnection.StartListening(stoppingToken);
            }

            _socket.Close();
        }

        private void AgentConnection_Connected(AgentConnection agentConnection)
        {
            Connections.TryAdd(agentConnection.ConnectionId, agentConnection);

            _logger.LogInformation($"Agent connected: {agentConnection.AgentInstance!.InstanceName}");
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

            _logger.LogInformation($"Agent disconnected: {agentConnection.AgentInstance!.InstanceName}");
        }

        private void AgentConnection_SubscribedForCommands(AgentConnection agentConnection)
        {
            _commandsSubscribers.TryAdd(agentConnection.AgentInstance!.Id, agentConnection.ConnectionId);
        }

        public bool IsConnected(Guid agentId)
            => _commandsSubscribers.ContainsKey(agentId);

        public List<Agent> GetConnectedAgents(List<Agent> agents)
            => agents.Where(c => _commandsSubscribers.ContainsKey(c.Id)).ToList();

        public async Task UpdateTechLogSeances(List<Agent> agents, CancellationToken cancellationToken)
        {
            foreach(var agent in agents)
            {
                if (_commandsSubscribers.TryGetValue(agent.Id, out var connectionId) && Connections.TryGetValue(connectionId, out var connection))
                {
                    try
                    {
                        await connection.UpdateTechLogSeances(null, cancellationToken);
                    }
                    catch { }
                }
            }
        }
    }
}
