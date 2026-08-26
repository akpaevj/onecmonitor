using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Extensions;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/techlog/seances")]
public class TechLogSeancesController(AppDbContext dbContext, AgentsConnectionsManager agentsConnectionsManager) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<IReadOnlyList<TechLogSeanceListItem>> GetList(CancellationToken cancellationToken)
    {
        var rows = await dbContext.TechLogSeances
            .AsNoTracking()
            .Include(s => s.Agents)
            .Include(s => s.Templates)
            .OrderByDescending(s => s.StartDateTime)
            .ThenBy(s => s.Description)
            .ToListAsync(cancellationToken);

        return rows.Select(ToListItem).ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<ActionResult<TechLogSeanceListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.TechLogSeances
            .AsNoTracking()
            .Include(s => s.Agents)
            .Include(s => s.Templates)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(ToListItem(item));
    }

    [HttpGet("lookups")]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<ActionResult<TechLogSeanceLookupsResponse>> GetLookups(CancellationToken cancellationToken)
    {
        var agents = await dbContext.Agents
            .AsNoTracking()
            .OrderBy(a => a.InstanceName)
            .Select(a => new TechLogLookupItem(a.Id, a.InstanceName))
            .ToListAsync(cancellationToken);

        var templates = await dbContext.LogTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TechLogLookupItem(t.Id, t.Name))
            .ToListAsync(cancellationToken);

        return Ok(new TechLogSeanceLookupsResponse(agents, templates));
    }

    [HttpPost]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<ActionResult<TechLogSeanceListItem>> Create([FromBody] UpsertTechLogSeanceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var selectedAgentIds = request.AgentIds.Distinct().ToList();
        var selectedTemplateIds = request.TemplateIds.Distinct().ToList();

        var selectedAgents = await dbContext.Agents
            .Where(a => selectedAgentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var selectedTemplates = await dbContext.LogTemplates
            .Where(t => selectedTemplateIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (selectedAgents.Count != selectedAgentIds.Count)
            return BadRequest("Указаны несуществующие агенты");

        if (selectedTemplates.Count != selectedTemplateIds.Count)
            return BadRequest("Указаны несуществующие шаблоны");

        var entity = new TechLogSeance
        {
            Description = request.Description.Trim(),
            StartMode = request.StartMode,
            StartDateTime = NormalizeStartDateTime(request.StartMode, request.StartDateTime),
            Duration = request.Duration,
            Agents = selectedAgents,
            Templates = selectedTemplates
        };

        dbContext.TechLogSeances.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await agentsConnectionsManager.RaiseUpdateSettings(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<ActionResult<TechLogSeanceListItem>> Update(Guid id, [FromBody] UpsertTechLogSeanceRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.TechLogSeances
            .Include(s => s.Agents)
            .Include(s => s.Templates)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var selectedAgentIds = request.AgentIds.Distinct().ToList();
        var selectedTemplateIds = request.TemplateIds.Distinct().ToList();

        var selectedAgents = await dbContext.Agents
            .Where(a => selectedAgentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var selectedTemplates = await dbContext.LogTemplates
            .Where(t => selectedTemplateIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (selectedAgents.Count != selectedAgentIds.Count)
            return BadRequest("Указаны несуществующие агенты");

        if (selectedTemplates.Count != selectedTemplateIds.Count)
            return BadRequest("Указаны несуществующие шаблоны");

        entity.Description = request.Description.Trim();
        entity.StartMode = request.StartMode;
        entity.StartDateTime = NormalizeStartDateTime(request.StartMode, request.StartDateTime);
        entity.Duration = request.Duration;

        entity.Agents.Clear();
        foreach (var agent in selectedAgents)
            entity.Agents.Add(agent);

        entity.Templates.Clear();
        foreach (var template in selectedTemplates)
            entity.Templates.Add(template);

        await dbContext.SaveChangesAsync(cancellationToken);

        await agentsConnectionsManager.RaiseUpdateSettings(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TechLogSeances.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        dbContext.TechLogSeances.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await agentsConnectionsManager.RaiseUpdateSettings(cancellationToken);

        return NoContent();
    }

    private static ActionResult? ValidateRequest(UpsertTechLogSeanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return new BadRequestObjectResult("Не заполнено описание");

        if (request.AgentIds.Count == 0)
            return new BadRequestObjectResult("Не указаны подключаемые агенты");

        if (request.TemplateIds.Count == 0)
            return new BadRequestObjectResult("Не указаны подключаемые шаблоны");

        if (request.StartMode != TechLogSeanceStartMode.Monitor && request.Duration <= 1)
            return new BadRequestObjectResult("Длительность не может быть меньше 1 минуты");

        if (request.StartMode == TechLogSeanceStartMode.Scheduled)
        {
            if (request.StartDateTime == DateTime.MinValue)
                return new BadRequestObjectResult("Для запланированного запуска укажите дату и время начала");

            if (request.StartDateTime <= DateTime.Now)
                return new BadRequestObjectResult("Дата и время начала не могут быть меньше текущей даты");
        }

        return null;
    }

    private static DateTime NormalizeStartDateTime(TechLogSeanceStartMode startMode, DateTime startDateTime)
    {
        return startMode switch
        {
            TechLogSeanceStartMode.Monitor => DateTime.MinValue,
            TechLogSeanceStartMode.Immediately => DateTime.UtcNow,
            _ => startDateTime
        };
    }

    private static TechLogSeanceListItem ToListItem(TechLogSeance item)
    {
        return new TechLogSeanceListItem(
            item.Id,
            item.Description,
            item.StartMode.ToString(),
            item.StartMode.GetDisplay(),
            item.StartDateTime,
            item.Duration,
            item.Agents.Select(a => a.Id).ToList(),
            item.Templates.Select(t => t.Id).ToList());
    }

    public sealed record UpsertTechLogSeanceRequest(
        string Description,
        TechLogSeanceStartMode StartMode,
        DateTime StartDateTime,
        int Duration,
        IReadOnlyList<Guid> AgentIds,
        IReadOnlyList<Guid> TemplateIds);

    public sealed record TechLogSeanceListItem(
        Guid Id,
        string Description,
        string StartMode,
        string StartModeDisplay,
        DateTime StartDateTime,
        int Duration,
        IReadOnlyList<Guid> AgentIds,
        IReadOnlyList<Guid> TemplateIds);

    public sealed record TechLogLookupItem(Guid Id, string Name);

    public sealed record TechLogSeanceLookupsResponse(
        IReadOnlyList<TechLogLookupItem> Agents,
        IReadOnlyList<TechLogLookupItem> Templates);
}
