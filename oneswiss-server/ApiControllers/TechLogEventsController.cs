using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Models;
using OneSwiss.Common.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/techlog/events")]
public class TechLogEventsController(
    AppDbContext dbContext,
    TechLogRepositoryManager repositoryManager) : ControllerBase
{
    [HttpGet("seances/{seanceId:guid}")]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<ActionResult<TechLogSeanceEventsResponse>> GetBySeance(
        Guid seanceId,
        [FromQuery] int pageSize = 30,
        [FromQuery] int page = 0,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            return BadRequest("Размер страницы должен быть больше 0");

        if (page < 0)
            return BadRequest("Номер страницы не может быть отрицательным");

        var seance = await dbContext.TechLogSeances
            .AsNoTracking()
            .Where(c => c.Id == seanceId)
            .Select(c => new { c.Id, c.Description })
            .SingleOrDefaultAsync(cancellationToken);

        if (seance == null)
            return NotFound();

        var fullFilter = BuildFilter(seanceId, filter);

        try
        {
            using var repository = repositoryManager.GetInstance();

            var items = await repository.GetTjEvents(pageSize, page * pageSize, fullFilter, cancellationToken);
            var totalItems = await repository.GetRowsCount(fullFilter, cancellationToken);

            return Ok(new TechLogSeanceEventsResponse(
                seance.Id,
                seance.Description,
                page,
                pageSize,
                totalItems,
                items.Select(ToItem).ToList()));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("seances/{seanceId:guid}/timeline")]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<ActionResult<TechLogSeanceTimelineResponse>> GetTimeline(
        Guid seanceId,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var seance = await dbContext.TechLogSeances
            .AsNoTracking()
            .Where(c => c.Id == seanceId)
            .Select(c => new { c.Id, c.Description })
            .SingleOrDefaultAsync(cancellationToken);

        if (seance == null)
            return NotFound();

        var fullFilter = BuildFilter(seanceId, filter);

        try
        {
            using var repository = repositoryManager.GetInstance();

            var items = await repository.GetTjEvents(fullFilter, cancellationToken);

            var timelineItems = items
                .OrderBy(c => c.DateTime)
                .Select(c => new TechLogTimelineItem(
                    c.Id,
                    c.StartDateTime,
                    c.DateTime,
                    c.Duration,
                    c.EventName,
                    c.Level))
                .ToList();

            return Ok(new TechLogSeanceTimelineResponse(seance.Id, seance.Description, timelineItems));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private static string BuildFilter(Guid seanceId, string? filter)
    {
        var baseFilter = $"SeanceId = toUUID('{seanceId}')";
        var userFilter = filter?.Trim();

        return string.IsNullOrWhiteSpace(userFilter)
            ? baseFilter
            : $"{baseFilter} and {userFilter}";
    }

    private static TechLogEventItem ToItem(TjEvent item)
    {
        return new TechLogEventItem(
            item.Id,
            item.DateTime,
            item.Duration,
            item.EventName,
            item.Level,
            item.Properties);
    }

    public sealed record TechLogSeanceEventsResponse(
        Guid SeanceId,
        string SeanceDescription,
        int Page,
        int PageSize,
        int TotalItems,
        IReadOnlyList<TechLogEventItem> Items);

    public sealed record TechLogEventItem(
        Guid Id,
        DateTime DateTime,
        long Duration,
        string EventName,
        int Level,
        IReadOnlyDictionary<string, string> Properties);

    public sealed record TechLogSeanceTimelineResponse(
        Guid SeanceId,
        string SeanceDescription,
        IReadOnlyList<TechLogTimelineItem> Items);

    public sealed record TechLogTimelineItem(
        Guid Id,
        DateTime StartDateTime,
        DateTime EndDateTime,
        long Duration,
        string EventName,
        int Level);
}
