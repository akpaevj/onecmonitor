using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO;
using OneSwiss.Server.Models;
using OneSwiss.V8.Platform;

namespace OneSwiss.Server.Services;

public class InterAgencyCommunicationService(
    IDbContextFactory<AppDbContext> contextFactory,
    AgentsConnectionsManager connectionsManager)
{
    public async Task<V8Platform> GetCrServerPlatform(CrServerPlatformRequestDto request,
        CancellationToken cancellationToken)
    {
        var agent = await GetAgentById(request.AgentId, cancellationToken);
        var connection = GetAgentConnection(agent);

        var services = await connection.GetCrServerServices(cancellationToken);
        var service = services.FirstOrDefault(c => c.Port == request.Port);
        if (service is not { IsActive: true })
            throw new Exception(
                $"На агенте {agent.InstanceName} не найдена активная служба сервера хранилища на порту {request.Port}");

        return service.Platform;
    }

    private AgentConnection GetAgentConnection(Agent agent)
    {
        var connection = connectionsManager.GetAgentConnection(agent);
        if (connection == null)
            throw new Exception($"Не найдено активное соединение агента {agent.InstanceName}");

        return connection;
    }

    private async Task<AgentConnection> GetAgentConnectionById(Guid id, CancellationToken cancellationToken)
    {
        var agent = await GetAgentById(id, cancellationToken);

        return GetAgentConnection(agent);
    }

    private async Task<Agent> GetAgentById(Guid id, CancellationToken cancellationToken)
    {
        var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var agent = context.Agents.FirstOrDefault(c => c.Id == id);
        if (agent == null)
            throw new Exception($"Агент с идентификатором {id} не найден");

        return agent;
    }
}