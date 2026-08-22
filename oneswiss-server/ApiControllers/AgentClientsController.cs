using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.ApiControllers;

// Управление client_credentials-клиентами для аутентификации агентов - см. AuthController.Token.
[ApiController]
[Route("api/agent-clients")]
public class AgentClientsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<AgentClientListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.AgentClients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AgentClientListItem(c.Id, c.Name, c.CreatedAt, c.LastUsedAt))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreateAgentClientResponse>> Create([FromBody] CreateAgentClientRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не заполнено наименование");

        var clientSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var entity = new AgentClient
        {
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        entity.ClientSecretHash = new PasswordHasher<AgentClient>().HashPassword(entity, clientSecret);

        dbContext.AgentClients.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CreateAgentClientResponse(entity.Id, entity.Name, clientSecret, entity.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AgentClients.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        dbContext.AgentClients.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    public sealed record AgentClientListItem(Guid Id, string Name, DateTime CreatedAt, DateTime? LastUsedAt);

    public sealed record CreateAgentClientRequest(string Name);

    // ClientSecret возвращается только здесь, один раз - хэш восстановить обратно нельзя.
    public sealed record CreateAgentClientResponse(Guid Id, string Name, string ClientSecret, DateTime CreatedAt);
}
