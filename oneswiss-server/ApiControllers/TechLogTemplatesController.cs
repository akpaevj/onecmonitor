using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/techlog/templates")]
public class TechLogTemplatesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<IReadOnlyList<TechLogTemplateListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.LogTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TechLogTemplateListItem(
                t.Id,
                t.Name,
                t.Content,
                LogTemplate.BuiltInTemplatesIds.Contains(t.Id)))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadTechLogSeances},{Roles.WriteTechLogSeances}")]
    public async Task<ActionResult<TechLogTemplateListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.LogTemplates
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TechLogTemplateListItem(
                t.Id,
                t.Name,
                t.Content,
                LogTemplate.BuiltInTemplatesIds.Contains(t.Id)))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<ActionResult<TechLogTemplateListItem>> Create([FromBody] UpsertTechLogTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var entity = new LogTemplate
        {
            Name = request.Name.Trim(),
            Content = request.Content
        };

        dbContext.LogTemplates.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new TechLogTemplateListItem(entity.Id, entity.Name, entity.Content, false));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<ActionResult<TechLogTemplateListItem>> Update(Guid id,
        [FromBody] UpsertTechLogTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.LogTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        if (LogTemplate.BuiltInTemplatesIds.Contains(id))
            return BadRequest("Встроенный шаблон нельзя изменять");

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        entity.Name = request.Name.Trim();
        entity.Content = request.Content;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new TechLogTemplateListItem(entity.Id, entity.Name, entity.Content, false));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.WriteTechLogSeances)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (LogTemplate.BuiltInTemplatesIds.Contains(id))
            return BadRequest("Встроенный шаблон нельзя удалять");

        var entity = await dbContext.LogTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        dbContext.LogTemplates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ActionResult? ValidateRequest(UpsertTechLogTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new BadRequestObjectResult("Не заполнено наименование");

        if (string.IsNullOrWhiteSpace(request.Content))
            return new BadRequestObjectResult("Не заполнен контент шаблона");

        return null;
    }

    public sealed record UpsertTechLogTemplateRequest(
        string Name,
        string Content);

    public sealed record TechLogTemplateListItem(
        Guid Id,
        string Name,
        string Content,
        bool IsBuiltIn);
}
