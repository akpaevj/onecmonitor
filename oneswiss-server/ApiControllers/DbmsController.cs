using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Models;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/dbms")]
public class DbmsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<DbmsListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.Dbms
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DbmsListItem(
                d.Id,
                d.Name,
                d.Type,
                d.Host,
                d.Port))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<DbmsListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Dbms
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DbmsListItem(
                d.Id,
                d.Name,
                d.Type,
                d.Host,
                d.Port))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DbmsListItem>> Create([FromBody] UpsertDbmsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var entity = new Models.Dbms
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Host = request.Host.Trim(),
            Port = request.Port
        };

        dbContext.Dbms.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<DbmsListItem>> Update(Guid id, [FromBody] UpsertDbmsRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Dbms.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.Host = request.Host.Trim();
        entity.Port = request.Port;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Dbms.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var usedInTechLogSettings = await dbContext.TechLogSettings
            .AsNoTracking()
            .AnyAsync(s => s.DbmsId == id, cancellationToken);

        if (usedInTechLogSettings)
            return BadRequest("СУБД используется в настройках техжурнала");

        var usedInEventLogSettings = await dbContext.EventLogSettings
            .AsNoTracking()
            .AnyAsync(s => s.DbmsId == id, cancellationToken);

        if (usedInEventLogSettings)
            return BadRequest("СУБД используется в настройках журнала регистрации");

        dbContext.Dbms.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ActionResult? ValidateRequest(UpsertDbmsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new BadRequestObjectResult("Не заполнено наименование");

        if (string.IsNullOrWhiteSpace(request.Host))
            return new BadRequestObjectResult("Не заполнен адрес сервера");

        if (request.Port <= 0)
            return new BadRequestObjectResult("Не заполнен порт сервера");

        return null;
    }

    private static DbmsListItem ToListItem(Models.Dbms item)
    {
        return new DbmsListItem(item.Id, item.Name, item.Type, item.Host, item.Port);
    }

    public sealed record DbmsListItem(
        Guid Id,
        string Name,
        DbmsType Type,
        string Host,
        int Port);

    public sealed record UpsertDbmsRequest(
        string Name,
        DbmsType Type,
        string Host,
        int Port);
}
