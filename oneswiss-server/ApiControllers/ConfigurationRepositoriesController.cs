using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/configurationrepositories")]
public class ConfigurationRepositoriesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<ConfigurationRepositoryListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.ConfigRepositories
            .AsNoTracking()
            .Include(c => c.Agent)
            .OrderBy(c => c.Name)
            .Select(c => new ConfigurationRepositoryListItem(
                c.Id,
                c.Name,
                c.Host,
                c.Port,
                c.Agent.InstanceName,
                c.Deleted))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<ConfigurationRepositoryDetails>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var repository = await dbContext.ConfigRepositories
            .AsNoTracking()
            .Include(c => c.Users)
            .Where(c => c.Id == id)
            .Select(c => new ConfigurationRepositoryDetails(
                c.Id,
                c.Name,
                c.Host,
                c.Port,
                c.CredentialsId,
                c.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new RepositoryUserItem(u.Id, u.Name, u.Deleted))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (repository == null)
            return NotFound();

        return Ok(repository);
    }

    [HttpGet("credentials")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<RepositoryCredentialsItem>> GetCredentials(CancellationToken cancellationToken)
    {
        return await dbContext.Credentials
            .AsNoTracking()
            .Where(c => !c.IsToken)
            .OrderBy(c => c.Name)
            .Select(c => new RepositoryCredentialsItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<ConfigurationRepositoryDetails>> Update(Guid id,
        [FromBody] UpdateConfigurationRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        var repository = await dbContext.ConfigRepositories
            .Include(c => c.Users)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (repository == null)
            return NotFound();

        if (request.CredentialsId == null || request.CredentialsId == Guid.Empty)
            return BadRequest("Не указаны учетные данные");

        var credentialsExists = await dbContext.Credentials
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CredentialsId && !c.IsToken, cancellationToken);

        if (!credentialsExists)
            return BadRequest("Указаны некорректные учетные данные");

        repository.CredentialsId = request.CredentialsId;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ConfigurationRepositoryDetails(
            repository.Id,
            repository.Name,
            repository.Host,
            repository.Port,
            repository.CredentialsId,
            repository.Users
                .OrderBy(u => u.Name)
                .Select(u => new RepositoryUserItem(u.Id, u.Name, u.Deleted))
                .ToList());

        return Ok(response);
    }

    public sealed record ConfigurationRepositoryListItem(
        Guid Id,
        string Name,
        string Host,
        int Port,
        string Agent,
        bool Deleted);

    public sealed record ConfigurationRepositoryDetails(
        Guid Id,
        string Name,
        string Host,
        int Port,
        Guid? CredentialsId,
        IReadOnlyList<RepositoryUserItem> Users);

    public sealed record RepositoryUserItem(
        Guid Id,
        string Name,
        bool Deleted);

    public sealed record RepositoryCredentialsItem(
        Guid Id,
        string Name);

    public sealed record UpdateConfigurationRepositoryRequest(
        Guid? CredentialsId);
}
