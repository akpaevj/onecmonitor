using OneSwiss.Agent.Models;
using OneSwiss.Common.DTO;
using OneSwiss.V8.Platform;

namespace OneSwiss.Agent.Services;

public class AgentsResourcesProvider(
    AgentInstance currentAgent,
    [FromKeyedServices(OneSwissConnection.CommonKey)]
    OneSwissConnection connection,
    V8ServicesProvider v8ServicesProvider)
{
    public async Task<V8Platform> GetCrServerPlatform(ConfigurationRepositoryDto repository,
        CancellationToken cancellationToken)
    {
        return await GetCrServerPlatform(repository.Agent.Id, repository.Port, cancellationToken);
    }

    public async Task<V8Platform> GetCrServerPlatform(Guid agentId, int port, CancellationToken cancellationToken)
    {
        if (agentId == currentAgent.Id)
            return v8ServicesProvider.GetCrServerForPort(port).Platform;

        return await connection.GetCrServerPlatform(agentId, port, cancellationToken);
    }
}