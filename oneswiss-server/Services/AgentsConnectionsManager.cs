using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services
{
    public class AgentsConnectionsManager(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
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

                try
                {
                    var client = await _socket.AcceptAsync(stoppingToken);

                    var agentConnection = new AgentConnection(
                        client,
                        serviceProvider,
                        serviceProvider.GetRequiredService<ILogger<AgentConnection>>());
                
                    agentConnection.AgentConnected += AgentConnection_Connected;
                    agentConnection.AgentDisconnected += AgentConnection_Disconnected;

                    agentConnection.Listen(stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка обработчики входящего подключения");
                }
            }

            _socket.Close();
        }
        
        public AgentConnection? GetAgentConnection(Guid agentId)
        {
            AgentConnection? agentConnection;
            
            lock (_connections)
                agentConnection = _connections.FirstOrDefault(c => c.AgentInstance!.MainConnection && c.AgentInstance.Id == agentId);

            return agentConnection;
        }
        
        public async Task<List<AgentConnection>> GetActiveAgentsConnections(CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var dbAgents = await dbContext.Agents.AsNoTracking().ToListAsync(cancellationToken);
            
            return GetActiveAgentsConnections(dbAgents);
        }
        
        public async Task RaiseUpdateSettings(CancellationToken cancellationToken)
        {
            var connections = await GetActiveAgentsConnections(cancellationToken);
        
            foreach (var connection in connections)
                await connection.SendSettingsRequest(cancellationToken);
        }
        
        public async Task StartMaintenanceTask(Guid id, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var task = await context.MaintenanceTasks
                .AsNoTracking()
                
                .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep.File)
                .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep.ConfigurationRepository.Credentials)
                
                .Include(c => c.Steps).ThenInclude(c => c.LoadExtensionStep.File)
                .Include(c => c.Steps).ThenInclude(c => c.LoadExtensionStep.ConfigurationRepository.Credentials)
                
                .Include(c => c.Steps).ThenInclude(c => c.UpdateConfigurationStep.File)
                
                .Include(c => c.Steps).ThenInclude(c => c.ExecuteOneScriptStep.File)
                
                .Include(c => c.Steps).ThenInclude(c => c.StartExternalDataProcessorStep.File)
                
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.SourceCredentials)
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.SourceInfoBase.Credentials)
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.SourceInfoBase.Cluster.Credentials)
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.DestinationCredentials)
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.DestinationInfoBase.Credentials)
                .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep).ThenInclude(c => c.DestinationInfoBase.Cluster.Credentials)
                
                .Include(c => c.Agents)
                .Include(c => c.InfoBases).ThenInclude(c => c.Credentials)
                .Include(c => c.InfoBases).ThenInclude(c => c.Cluster).ThenInclude(c => c.Agent)
                .Include(c => c.InfoBases).ThenInclude(c => c.Cluster).ThenInclude(c => c.Credentials)
                
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken);

            var taskAgents = task!.CommonDestination switch
            {
                false => task.InfoBases.Select(c => c.Cluster.Agent.Id).Distinct().ToList(),
                true => task.Agents.Select(c => c.Id).Distinct().ToList() 
            };  
            var connections = await GetActiveAgentsConnections(cancellationToken);
            
            task.StartDateTime = DateTime.Now;

            await context.SaveChangesAsync(cancellationToken);

            foreach (var connection in connections.Where(c => taskAgents.Contains(c.AgentInstance!.Id)))
                await connection.StartMaintenanceTask(task, cancellationToken);
        }

        private void AgentConnection_Connected(AgentConnection agentConnection)
        {
            lock (_connections)
                _connections.Add(agentConnection);

            if (agentConnection.AgentInstance?.MainConnection != true) 
                return;

            SendAgentStateChangedNotification();
            
            logger.LogInformation($"Агент подключился: {agentConnection.AgentInstance!.InstanceName}. Идентификатор соединения: {agentConnection.ConnectionId}");
        }

        private void AgentConnection_Disconnected(AgentConnection agentConnection)
        {
            lock (_connections)
                _connections.Remove(agentConnection);

            agentConnection.AgentConnected -= AgentConnection_Connected;
            agentConnection.AgentDisconnected -= AgentConnection_Disconnected;

            if (agentConnection.AgentInstance?.MainConnection != true) 
                return;

            SendAgentStateChangedNotification();
            
            logger.LogInformation($"Агент отключился: {agentConnection.AgentInstance!.InstanceName}. Идентификатор соединения: {agentConnection.ConnectionId}");
        }

        private void SendAgentStateChangedNotification()
        {
            using var scope = serviceProvider.CreateAsyncScope();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AgentConnectionsHub>>();
            hub.Clients.All.SendAsync("AgentsStateUpdated");
        }

        private List<AgentConnection> GetActiveAgentsConnections(List<Agent> agents)
        {
            return agents.Select(c => GetAgentConnection(c.Id)).Where(c => c != null).ToList()!;
        }
    }
}
