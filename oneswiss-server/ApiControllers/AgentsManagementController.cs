using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/agents")]
public class AgentsManagementController(AppDbContext dbContext, AgentsConnectionsManager connectionsManager) : ControllerBase
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
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Agents
            .Include(a => a.Clusters)
            .Include(a => a.TechLogSeances)
            .Include(a => a.MaintenanceTasks)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        if (entity.Clusters.Count > 0)
            return BadRequest("Нельзя удалить агент, к которому привязаны кластеры");

        if (entity.TechLogSeances.Count > 0)
            return BadRequest("Нельзя удалить агент, к которому привязаны сеансы техжурнала");

        if (entity.MaintenanceTasks.Count > 0)
            return BadRequest("Нельзя удалить агент, к которому привязаны задачи обслуживания");

        dbContext.Agents.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
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
