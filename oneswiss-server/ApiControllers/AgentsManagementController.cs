using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.DTO;
using OneSwiss.Common.Helpers;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/agents")]
public class AgentsManagementController(
    AppDbContext dbContext,
    AgentsConnectionsManager connectionsManager,
    IMapper mapper,
    ILogger<AgentsManagementController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<AgentListItem>> GetList(CancellationToken cancellationToken)
    {
        var agents = await dbContext.Agents
            .AsNoTracking()
            .OrderBy(a => a.InstanceName)
            .Select(a => new
            {
                a.Id,
                a.InstanceName,
                ClustersCount = a.Clusters.Count,
                TechLogSeancesCount = a.TechLogSeances.Count,
                MaintenanceTasksCount = a.MaintenanceTasks.Count
            })
            .ToListAsync(cancellationToken);

        return agents.Select(a => new AgentListItem(
                a.Id,
                a.InstanceName,
                connectionsManager.GetAgentConnection(a.Id) != null,
                a.ClustersCount,
                a.TechLogSeancesCount,
                a.MaintenanceTasksCount))
            .ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<AgentListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Agents
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.InstanceName,
                ClustersCount = a.Clusters.Count,
                TechLogSeancesCount = a.TechLogSeances.Count,
                MaintenanceTasksCount = a.MaintenanceTasks.Count
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(new AgentListItem(
            item.Id,
            item.InstanceName,
            connectionsManager.GetAgentConnection(item.Id) != null,
            item.ClustersCount,
            item.TechLogSeancesCount,
            item.MaintenanceTasksCount));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AgentListItem>> Create([FromBody] UpsertAgentRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, null, cancellationToken);
        if (validationError != null)
            return validationError;

        var entity = new Models.Agent
        {
            InstanceName = request.InstanceName.Trim()
        };

        dbContext.Agents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new AgentListItem(entity.Id, entity.InstanceName, false, 0, 0, 0));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<AgentListItem>> Update(Guid id, [FromBody] UpsertAgentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Agents
            .Include(a => a.Clusters)
            .Include(a => a.TechLogSeances)
            .Include(a => a.MaintenanceTasks)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        var validationError = await ValidateRequest(request, id, cancellationToken);
        if (validationError != null)
            return validationError;

        entity.InstanceName = request.InstanceName.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AgentListItem(
            entity.Id,
            entity.InstanceName,
            connectionsManager.GetAgentConnection(entity.Id) != null,
            entity.Clusters.Count,
            entity.TechLogSeances.Count,
            entity.MaintenanceTasks.Count));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Agents
            .Include(a => a.Clusters)
            .ThenInclude(c => c.InfoBases)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        // Захватываем внутренние идентификаторы ИБ до удаления - после каскадного удаления
        // кластеров/ИБ в Postgres они станут недоступны, а именно по ним хранятся события
        // журнала регистрации в ClickHouse.
        var infoBaseInternalIds = entity.Clusters
            .SelectMany(c => c.InfoBases)
            .Select(c => c.InfoBaseInternalId)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        // Разрываем активное соединение, иначе живой агент-процесс может воскресить запись
        // об агенте своим ближайшим heartbeat-сообщением (см. AgentConnection.HandleInitMessage).
        connectionsManager.GetAgentConnection(id)?.Dispose();

        // Кластеры, ИБ и связи с сеансами ТЖ/задачами обслуживания удаляются каскадно на
        // уровне БД (см. конфигурацию FK в AppDbContextModelSnapshot). Сами сеансы ТЖ и
        // задачи обслуживания не удаляются - они могут быть общими для нескольких агентов.
        dbContext.Agents.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await DeleteTechLogAgentData(id, cancellationToken);
        await DeleteEventLogInfoBasesData(infoBaseInternalIds, cancellationToken);

        return NoContent();
    }

    private async Task DeleteTechLogAgentData(Guid agentId, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await dbContext.TechLogSettings
                .AsNoTracking()
                .Include(c => c.Dbms)
                .Include(c => c.Credentials)
                .SingleOrDefaultAsync(cancellationToken);

            if (settings == null)
                return;

            var settingsDto = mapper.Map<TechLogSettingsDto>(settings);
            using var repository = settingsDto.GetDbContext();
            await repository.DeleteAgentData(agentId.ToString(), cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Не удалось удалить данные технологического журнала агента {AgentId} в ClickHouse", agentId);
        }
    }

    private async Task DeleteEventLogInfoBasesData(IReadOnlyCollection<string> infoBaseInternalIds,
        CancellationToken cancellationToken)
    {
        if (infoBaseInternalIds.Count == 0)
            return;

        try
        {
            var settings = await dbContext.EventLogSettings
                .AsNoTracking()
                .Include(c => c.Dbms)
                .Include(c => c.Credentials)
                .OrderBy(c => c.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (settings == null)
                return;

            var settingsDto = mapper.Map<EventLogSettingsDto>(settings);
            using var repository = settingsDto.GetDbContext();

            foreach (var infoBaseInternalId in infoBaseInternalIds)
                await repository.DeleteInfoBaseData(infoBaseInternalId, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Не удалось удалить данные журнала регистрации удаленных ИБ в ClickHouse");
        }
    }

    private async Task<ActionResult?> ValidateRequest(UpsertAgentRequest request, Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InstanceName))
            return BadRequest("Не заполнено имя инстанса");

        var instanceName = request.InstanceName.Trim();
        var exists = await dbContext.Agents
            .AsNoTracking()
            .AnyAsync(a => a.InstanceName == instanceName && (currentId == null || a.Id != currentId), cancellationToken);

        if (exists)
            return BadRequest("Агент с таким именем уже существует");

        return null;
    }

    public sealed record AgentListItem(
        Guid Id,
        string InstanceName,
        bool IsConnected,
        int ClustersCount,
        int TechLogSeancesCount,
        int MaintenanceTasksCount);

    public sealed record UpsertAgentRequest(string InstanceName);
}
