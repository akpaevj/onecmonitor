using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/credentials")]
public class CredentialsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<CredentialsListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CredentialsListItem(
                c.Id,
                c.Name,
                c.IsToken,
                c.Token,
                c.User,
                c.Password,
                c.DefaultForClusters,
                c.DefaultV8Admin,
                c.DefaultConfigRepositoriesAdmin))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<CredentialsListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Credentials
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CredentialsListItem(
                c.Id,
                c.Name,
                c.IsToken,
                c.Token,
                c.User,
                c.Password,
                c.DefaultForClusters,
                c.DefaultV8Admin,
                c.DefaultConfigRepositoriesAdmin))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CredentialsListItem>> Create([FromBody] UpsertCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        var entity = new Models.Credentials
        {
            Name = request.Name.Trim(),
            IsToken = request.IsToken,
            Token = request.IsToken ? request.Token.Trim() : string.Empty,
            User = request.IsToken ? string.Empty : request.User.Trim(),
            Password = request.IsToken ? string.Empty : request.Password,
            DefaultForClusters = request.IsToken ? false : request.DefaultForClusters,
            DefaultV8Admin = request.IsToken ? false : request.DefaultV8Admin,
            DefaultConfigRepositoriesAdmin = request.IsToken ? false : request.DefaultConfigRepositoriesAdmin
        };

        dbContext.Credentials.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToListItem(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<CredentialsListItem>> Update(Guid id, [FromBody] UpsertCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Credentials.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return validationError;

        entity.Name = request.Name.Trim();
        entity.IsToken = request.IsToken;
        entity.Token = request.IsToken ? request.Token.Trim() : string.Empty;
        entity.User = request.IsToken ? string.Empty : request.User.Trim();
        entity.Password = request.IsToken ? string.Empty : request.Password;
        entity.DefaultForClusters = request.IsToken ? false : request.DefaultForClusters;
        entity.DefaultV8Admin = request.IsToken ? false : request.DefaultV8Admin;
        entity.DefaultConfigRepositoriesAdmin = request.IsToken ? false : request.DefaultConfigRepositoriesAdmin;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Credentials.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var isUsedInClusters = await dbContext.Clusters.AsNoTracking()
            .AnyAsync(c => c.CredentialsId == id, cancellationToken);
        if (isUsedInClusters)
            return BadRequest("Учетные данные используются в кластерах");

        var isUsedInInfoBases = await dbContext.InfoBases.AsNoTracking()
            .AnyAsync(c => c.CredentialsId == id, cancellationToken);
        if (isUsedInInfoBases)
            return BadRequest("Учетные данные используются в информационных базах");

        var isUsedInConfigRepositories = await dbContext.ConfigRepositories.AsNoTracking()
            .AnyAsync(c => c.CredentialsId == id, cancellationToken);
        if (isUsedInConfigRepositories)
            return BadRequest("Учетные данные используются в хранилищах конфигураций");

        var isUsedInTechLog = await dbContext.TechLogSettings.AsNoTracking()
            .AnyAsync(c => c.CredentialsId == id, cancellationToken);
        if (isUsedInTechLog)
            return BadRequest("Учетные данные используются в настройках техжурнала");

        var isUsedInEventLog = await dbContext.EventLogSettings.AsNoTracking()
            .AnyAsync(c => c.CredentialsId == id, cancellationToken);
        if (isUsedInEventLog)
            return BadRequest("Учетные данные используются в настройках журнала регистрации");

        var isUsedInMaintenanceSteps = await dbContext.MaintenanceSteps.AsNoTracking()
            .AnyAsync(s =>
                    (s.CopyInfoBaseStep != null && s.CopyInfoBaseStep.SourceCredentialsId == id) ||
                    (s.CopyInfoBaseStep != null && s.CopyInfoBaseStep.DestinationCredentialsId == id),
                cancellationToken);
        if (isUsedInMaintenanceSteps)
            return BadRequest("Учетные данные используются в шагах задач обслуживания");

        dbContext.Credentials.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ActionResult? ValidateRequest(UpsertCredentialsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new BadRequestObjectResult("Не заполнено наименование");

        if (request.IsToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return new BadRequestObjectResult("Не заполнен токен");

            return null;
        }

        if (string.IsNullOrWhiteSpace(request.User))
            return new BadRequestObjectResult("Не заполнен пользователь");

        if (string.IsNullOrWhiteSpace(request.Password))
            return new BadRequestObjectResult("Не заполнен пароль");

        return null;
    }

    private static CredentialsListItem ToListItem(Models.Credentials c)
    {
        return new CredentialsListItem(
            c.Id,
            c.Name,
            c.IsToken,
            c.Token,
            c.User,
            c.Password,
            c.DefaultForClusters,
            c.DefaultV8Admin,
            c.DefaultConfigRepositoriesAdmin);
    }

    public sealed record CredentialsListItem(
        Guid Id,
        string Name,
        bool IsToken,
        string Token,
        string User,
        string? Password,
        bool DefaultForClusters,
        bool DefaultV8Admin,
        bool DefaultConfigRepositoriesAdmin);

    public sealed record UpsertCredentialsRequest(
        string Name,
        bool IsToken,
        string Token,
        string User,
        string Password,
        bool DefaultForClusters,
        bool DefaultV8Admin,
        bool DefaultConfigRepositoriesAdmin);
}
