using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/mcp-clients")]
public class McpClientsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<McpClientListItem>> GetList(CancellationToken cancellationToken)
    {
        var clients = await dbContext.OAuthClients
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var activeCounts = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.OAuthClientId != null && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .GroupBy(t => t.OAuthClientId!.Value)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ClientId, g => g.Count, cancellationToken);

        return clients
            .Select(c => new McpClientListItem(
                c.Id,
                c.ClientName,
                c.CreatedAtUtc,
                JsonSerializer.Deserialize<List<string>>(c.RedirectUrisJson) ?? [],
                activeCounts.GetValueOrDefault(c.Id)))
            .ToList();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var client = await dbContext.OAuthClients.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client == null)
            return NotFound();

        await dbContext.RefreshTokens
            .Where(t => t.OAuthClientId == id && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), cancellationToken);

        await dbContext.OAuthAuthorizationCodes
            .Where(c => c.ClientId == id)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.OAuthClients.Remove(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    public sealed record McpClientListItem(
        Guid Id,
        string? ClientName,
        DateTime CreatedAtUtc,
        IReadOnlyList<string> RedirectUris,
        int ActiveSessionsCount);
}
