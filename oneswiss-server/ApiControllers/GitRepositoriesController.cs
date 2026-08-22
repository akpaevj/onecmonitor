using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/gitrepositories")]
public class GitRepositoriesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadGitRepositories},{Roles.WriteGitRepositories}")]
    public async Task<GitRepositoriesResponse> GetList(CancellationToken cancellationToken)
    {
        var items = await dbContext.GitRepositories
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new GitRepositoryListItem(
                r.Id,
                r.Name,
                r.Address,
                r.TokenId))
            .ToListAsync(cancellationToken);

        return new GitRepositoriesResponse(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadGitRepositories},{Roles.WriteGitRepositories}")]
    public async Task<ActionResult<GitRepositoryListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.GitRepositories
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new GitRepositoryListItem(
                r.Id,
                r.Name,
                r.Address,
                r.TokenId))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("tokens")]
    [Authorize(Roles = $"{Roles.ReadGitRepositories},{Roles.WriteGitRepositories}")]
    public async Task<IReadOnlyList<GitTokenItem>> GetTokens(CancellationToken cancellationToken)
    {
        return await dbContext.Credentials
            .AsNoTracking()
            .Where(c => c.IsToken)
            .OrderBy(c => c.Name)
            .Select(c => new GitTokenItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.WriteGitRepositories)]
    public async Task<ActionResult<GitRepositoryListItem>> Create([FromBody] UpsertGitRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        var entity = new Models.GitRepository
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            TokenId = request.TokenId
        };

        dbContext.GitRepositories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new GitRepositoryListItem(entity.Id, entity.Name, entity.Address, entity.TokenId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteGitRepositories)]
    public async Task<ActionResult<GitRepositoryListItem>> Update(Guid id, [FromBody] UpsertGitRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.GitRepositories.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        entity.Name = request.Name.Trim();
        entity.Address = request.Address.Trim();
        entity.TokenId = request.TokenId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new GitRepositoryListItem(entity.Id, entity.Name, entity.Address, entity.TokenId));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.WriteGitRepositories)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GitRepositories.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        dbContext.GitRepositories.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<ActionResult?> ValidateRequest(UpsertGitRepositoryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не заполнено наименование");

        if (string.IsNullOrWhiteSpace(request.Address))
            return BadRequest("Не заполнен адрес");

        var tokenExists = await dbContext.Credentials
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.TokenId && c.IsToken, cancellationToken);

        if (!tokenExists)
            return BadRequest("Не задан корректный токен аутентификации");

        return null;
    }

    public sealed record GitRepositoriesResponse(
        IReadOnlyList<GitRepositoryListItem> Items);

    public sealed record GitRepositoryListItem(
        Guid Id,
        string Name,
        string Address,
        Guid TokenId);

    public sealed record GitTokenItem(
        Guid Id,
        string Name);

    public sealed record UpsertGitRepositoryRequest(
        string Name,
        string Address,
        Guid TokenId);
}
