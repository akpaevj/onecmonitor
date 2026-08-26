using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/accessgroups")]
public class AccessGroupsController(AppDbContext dbContext, UserGroupsManager groupsManager) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyList<AccessGroupListItem>> GetList(CancellationToken cancellationToken)
    {
        return await dbContext.AccessGroups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new AccessGroupListItem(
                g.Id,
                g.Name,
                g.IsBuiltIn,
                g.Roles.Select(r => r.Id).ToList(),
                g.Roles.Select(r => new RoleItem(r.Id, r.Name!, r.Description)).ToList(),
                g.UsersGroups.Count))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<AccessGroupListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.AccessGroups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new AccessGroupListItem(
                g.Id,
                g.Name,
                g.IsBuiltIn,
                g.Roles.Select(r => r.Id).ToList(),
                g.Roles.Select(r => new RoleItem(r.Id, r.Name!, r.Description)).ToList(),
                g.UsersGroups.Count))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("roles")]
    [Authorize]
    public async Task<IReadOnlyList<RoleItem>> GetRoles(CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleItem(r.Id, r.Name!, r.Description))
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AccessGroupListItem>> Create([FromBody] UpsertAccessGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        var roles = await dbContext.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var entity = new Models.AccessGroup
        {
            Name = request.Name.Trim(),
            IsBuiltIn = false,
            Roles = roles,
            UsersGroups = []
        };

        dbContext.AccessGroups.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await groupsManager.UpdateUsersRoles();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new AccessGroupListItem(
                entity.Id,
                entity.Name,
                entity.IsBuiltIn,
                roles.Select(r => r.Id).ToList(),
                roles.Select(r => new RoleItem(r.Id, r.Name!, r.Description)).ToList(),
                0));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<AccessGroupListItem>> Update(Guid id, [FromBody] UpsertAccessGroupRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.AccessGroups
            .Include(g => g.Roles)
            .Include(g => g.UsersGroups)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        if (entity.IsBuiltIn)
            return BadRequest("Встроенную группу нельзя изменять");

        var validationError = await ValidateRequest(request, cancellationToken);
        if (validationError != null)
            return validationError;

        var roles = await dbContext.Roles
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        entity.Name = request.Name.Trim();
        entity.Roles.Clear();
        entity.Roles.AddRange(roles);

        await dbContext.SaveChangesAsync(cancellationToken);
        await groupsManager.UpdateUsersRoles();

        return Ok(new AccessGroupListItem(
            entity.Id,
            entity.Name,
            entity.IsBuiltIn,
            roles.Select(r => r.Id).ToList(),
            roles.Select(r => new RoleItem(r.Id, r.Name!, r.Description)).ToList(),
            entity.UsersGroups.Count));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AccessGroups
            .Include(g => g.UsersGroups)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        if (entity.IsBuiltIn)
            return BadRequest("Встроенную группу нельзя удалять");

        if (entity.UsersGroups.Count > 0)
            return BadRequest("Нельзя удалить группу доступа, привязанную к группам пользователей");

        dbContext.AccessGroups.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await groupsManager.UpdateUsersRoles();

        return NoContent();
    }

    private async Task<ActionResult?> ValidateRequest(UpsertAccessGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не заполнено наименование");

        var roleIds = request.RoleIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (roleIds.Length == 0)
            return BadRequest("Не выбраны роли");

        var existingCount = await dbContext.Roles
            .AsNoTracking()
            .CountAsync(r => roleIds.Contains(r.Id), cancellationToken);

        if (existingCount != roleIds.Length)
            return BadRequest("Указаны несуществующие роли");

        return null;
    }

    public sealed record AccessGroupListItem(
        Guid Id,
        string Name,
        bool IsBuiltIn,
        IReadOnlyList<Guid> RoleIds,
        IReadOnlyList<RoleItem> Roles,
        int UsersGroupsCount);

    public sealed record RoleItem(
        Guid Id,
        string Name,
        string Description);

    public sealed record UpsertAccessGroupRequest(
        string Name,
        IReadOnlyList<Guid> RoleIds);
}
