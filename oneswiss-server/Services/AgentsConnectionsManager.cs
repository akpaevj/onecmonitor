using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO.MaintenanceTasks;
using OneSwiss.Common.Models.MaintenanceTasks;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Models;
using OneSwiss.Server.Models.MaintenanceTasks;
using OneSwiss.V8.Platform;

namespace OneSwiss.Server.Services
{
    public class AgentsConnectionsManager(
        IServiceProvider serviceProvider,
        IMapper mapper,
        ILogger<AgentsConnectionsManager> logger)
    {
        private readonly HashSet<AgentConnection> _connections = [];

        public void AcceptAgent(WebSocket socket, TaskCompletionSource cts)
        {
            try
            {
                var agentConnection = new AgentConnection(
                    socket,
                    serviceProvider,
                    serviceProvider.GetRequiredService<ILogger<AgentConnection>>());
                
                agentConnection.AgentConnected += AgentConnection_Connected;
                agentConnection.AgentDisconnected += conn =>
                {
                    cts.TrySetResult();
                    AgentConnection_Disconnected(conn);
                };

                agentConnection.Listen();
            }
            catch (Exception e)
            {
                cts.TrySetException(e);
                logger.LogError(e, "Ошибка обработчики входящего подключения");
            }
        }

        public AgentConnection? GetAgentConnection(Agent agent)
            => GetAgentConnection(agent.Id);
        
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

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var task = await context.MaintenanceTasks
                    .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep.File)
                    .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep.ConfigurationRepository.Agent)
                    .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep.ConfigurationRepository.Credentials)
                    
                    .Include(c => c.Steps).ThenInclude(c => c.LoadExtensionStep.File)
                    .Include(c => c.Steps).ThenInclude(c => c.LoadExtensionStep.ConfigurationRepository.Agent)
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
                
                task!.StartDateTime = DateTime.Now;
                await context.SaveChangesAsync(cancellationToken);
                
                var taskDto = mapper.Map<MaintenanceTaskDto>(task);
                
                // Заполним версии платформ хранилищ
                var repositoriesPlatforms = await GetStepsConfigRepositoriesPlatforms(task, cancellationToken);
                taskDto.Steps.ForEach(c =>
                {
                    if (c.Kind == MaintenanceStepKind.LoadConfiguration && c.LoadConfigurationStep!.FromConfigRepository)
                        c.LoadConfigurationStep!.ConfigurationRepository!.Platform = repositoriesPlatforms[c.LoadConfigurationStep.ConfigurationRepository.Id];
                    else if (c.Kind == MaintenanceStepKind.LoadExtension && c.LoadExtensionStep!.FromConfigRepository)
                        c.LoadExtensionStep!.ConfigurationRepository!.Platform = repositoriesPlatforms[c.LoadExtensionStep.ConfigurationRepository.Id];
                });
                
                var taskAgents = task.CommonDestination switch
                {
                    false => task.InfoBases.Select(c => c.Cluster.Agent.Id).Distinct().ToList(),
                    true => task.Agents.Select(c => c.Id).Distinct().ToList() 
                };  
                var connections = await GetActiveAgentsConnections(cancellationToken);

                foreach (var connection in connections.Where(c => taskAgents.Contains(c.AgentInstance!.Id)))
                    await connection.StartMaintenanceTask(taskDto, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<Dictionary<Guid, V8Platform>> GetStepsConfigRepositoriesPlatforms(MaintenanceTask task, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, V8Platform>();
            
            var stepsWithConfigurationRepositories = task.Steps.Where(c =>
                    c.LoadConfigurationStep?.FromConfigRepository == true ||
                    c.LoadExtensionStep?.FromConfigRepository == true)
                .ToList();

            foreach (var step in stepsWithConfigurationRepositories)
            {
                var repository = step.Kind switch
                {
                    MaintenanceStepKind.LoadConfiguration => step.LoadConfigurationStep!.ConfigurationRepository,
                    MaintenanceStepKind.LoadExtension => step.LoadExtensionStep!.ConfigurationRepository,
                    _ => null
                };

                if (repository == null) 
                    continue;
                
                var connection = GetAgentConnection(repository.Agent);
                if (connection == null)
                    throw new Exception(
                        $"Не удалось найти активное соединение с агентом {repository.Agent.InstanceName} при получении версии платформы сервера хранилищ");

                var repositoryDetails = await connection.GetConfigRepositoryDetails(repository.Port, repository.Name, cancellationToken);
                result.Add(repository.Id, repositoryDetails.Platform);
            }

            return result;
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
