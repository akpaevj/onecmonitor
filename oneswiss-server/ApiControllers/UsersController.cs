using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/users")]
public class UsersController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UserGroupsManager groupsManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("groups")]
    [Authorize]
    public async Task<IReadOnlyList<UsersGroupListItem>> GetGroups(CancellationToken cancellationToken)
    {
        return await dbContext.UsersGroups
            .AsNoTracking()
            .OrderBy(g => g.ParentId)
            .ThenBy(g => g.Name)
            .Select(g => new UsersGroupListItem(
                g.Id,
                g.Name,
                g.ParentId,
                g.IsBuiltIn,
                g.Users.Count))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("groups/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UsersGroupListItem>> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.UsersGroups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new UsersGroupListItem(
                g.Id,
                g.Name,
                g.ParentId,
                g.IsBuiltIn,
                g.Users.Count))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost("groups")]
    [Authorize]
    public async Task<ActionResult<UsersGroupListItem>> CreateGroup([FromBody] UpsertUsersGroupRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateGroupRequest(request, null, cancellationToken);
        if (validationError != null)
            return validationError;

        var entity = new UsersGroup
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId
        };

        dbContext.UsersGroups.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetGroupById), new { id = entity.Id },
            new UsersGroupListItem(entity.Id, entity.Name, entity.ParentId, entity.IsBuiltIn, 0));
    }

    [HttpPut("groups/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UsersGroupListItem>> UpdateGroup(Guid id, [FromBody] UpsertUsersGroupRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.UsersGroups.SingleOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        if (entity.IsBuiltIn)
            return BadRequest("Встроенную группу нельзя изменять");

        var validationError = await ValidateGroupRequest(request, id, cancellationToken);
        if (validationError != null)
            return validationError;

        entity.Name = request.Name.Trim();
        entity.ParentId = request.ParentId;

        await dbContext.SaveChangesAsync(cancellationToken);

        var usersCount = await dbContext.Users
            .AsNoTracking()
            .CountAsync(u => u.GroupId == id, cancellationToken);

        return Ok(new UsersGroupListItem(entity.Id, entity.Name, entity.ParentId, entity.IsBuiltIn, usersCount));
    }

    [HttpDelete("groups/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UsersGroups
            .Include(g => g.Users)
            .Include(g => g.Groups)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        if (entity.IsBuiltIn)
            return BadRequest("Встроенную группу нельзя удалять");

        if (entity.Users.Count > 0)
            return BadRequest("Нельзя удалить группу, в которой есть пользователи");

        if (entity.Groups.Count > 0)
            return BadRequest("Нельзя удалить группу, у которой есть дочерние группы");

        dbContext.UsersGroups.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("accounts")]
    [Authorize]
    public async Task<IReadOnlyList<UserAccountListItem>> GetAccounts([FromQuery] Guid? groupId, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Include(u => u.Group)
            .AsQueryable();

        if (groupId.HasValue)
        {
            var groupExists = await dbContext.UsersGroups
                .AsNoTracking()
                .AnyAsync(g => g.Id == groupId.Value, cancellationToken);

            if (!groupExists)
                return [];

            var groupIds = await GetGroupsWithChildren(groupId.Value, cancellationToken);
            query = query.Where(u => groupIds.Contains(u.GroupId));
        }

        return await query
            .OrderBy(u => u.UserName)
            .Select(u => new UserAccountListItem(
                u.Id,
                u.UserName ?? string.Empty,
                u.DisplayName,
                u.ExternalName,
                u.GroupId,
                u.Group.Name))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("accounts/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UserAccountListItem>> GetAccountById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Group)
            .Where(u => u.Id == id)
            .Select(u => new UserAccountListItem(
                u.Id,
                u.UserName ?? string.Empty,
                u.DisplayName,
                u.ExternalName,
                u.GroupId,
                u.Group.Name))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost("accounts")]
    [Authorize]
    public async Task<ActionResult<UserAccountListItem>> CreateAccount([FromBody] UpsertUserAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateAccountRequest(request, null, cancellationToken);
        if (validationError != null)
            return validationError;

        var groupId = request.GroupId == Guid.Empty ? BuiltInDbData.EveryoneGroup.Id : request.GroupId;

        var entity = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            NormalizedUserName = request.UserName.Trim().ToUpperInvariant(),
            GroupId = groupId,
            ExternalName = string.IsNullOrWhiteSpace(request.ExternalName) ? null : request.ExternalName.Trim(),
            DisplayName = request.DisplayName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var hasher = new PasswordHasher<ApplicationUser>();
        entity.PasswordHash = hasher.HashPassword(entity, request.Password ?? string.Empty);

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncExternalLogin(entity, cancellationToken);
        await groupsManager.UpdateUsersRoles();

        var group = await dbContext.UsersGroups.AsNoTracking().SingleAsync(g => g.Id == groupId, cancellationToken);

        return CreatedAtAction(nameof(GetAccountById), new { id = entity.Id },
            new UserAccountListItem(entity.Id, entity.UserName!, entity.DisplayName, entity.ExternalName, groupId, group.Name));
    }

    [HttpPut("accounts/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<UserAccountListItem>> UpdateAccount(Guid id, [FromBody] UpsertUserAccountRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var validationError = await ValidateAccountRequest(request, id, cancellationToken);
        if (validationError != null)
            return validationError;

        var groupId = request.GroupId == Guid.Empty ? BuiltInDbData.EveryoneGroup.Id : request.GroupId;

        var admins = await userManager.GetUsersInRoleAsync(Roles.Administrator);
        var currentAdmin = admins.FirstOrDefault(c => c.Id == entity.Id);
        if (currentAdmin != null && groupId != entity.GroupId)
            admins.Remove(currentAdmin);

        if (admins.Count == 0)
            return BadRequest("После изменения не останется пользователя с административными правами, объект не может быть записан");

        entity.UserName = request.UserName.Trim();
        entity.NormalizedUserName = request.UserName.Trim().ToUpperInvariant();
        entity.GroupId = groupId;
        entity.DisplayName = request.DisplayName;
        entity.ExternalName = string.IsNullOrWhiteSpace(request.ExternalName) ? null : request.ExternalName.Trim();
        entity.SecurityStamp = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(request.Password))
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            entity.PasswordHash = hasher.HashPassword(entity, request.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SyncExternalLogin(entity, cancellationToken);
        await groupsManager.UpdateUsersRoles();

        var group = await dbContext.UsersGroups.AsNoTracking().SingleAsync(g => g.Id == groupId, cancellationToken);

        return Ok(new UserAccountListItem(entity.Id, entity.UserName!, entity.DisplayName, entity.ExternalName, groupId, group.Name));
    }

    [HttpDelete("accounts/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var admins = await userManager.GetUsersInRoleAsync(Roles.Administrator);
        var currentAdmin = admins.FirstOrDefault(c => c.Id == entity.Id);
        if (currentAdmin != null)
            admins.Remove(currentAdmin);

        if (admins.Count == 0)
            return BadRequest("После удаления не останется пользователя с административными правами, удаление запрещено");

        var userLogins = dbContext.UserLogins.Where(c => c.UserId == entity.Id);
        dbContext.UserLogins.RemoveRange(userLogins);

        dbContext.Users.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await groupsManager.UpdateUsersRoles();

        return NoContent();
    }

    private async Task SyncExternalLogin(ApplicationUser user, CancellationToken cancellationToken)
    {
        var logins = dbContext.UserLogins.Where(c => c.UserId == user.Id);
        dbContext.UserLogins.RemoveRange(logins);

        if (string.IsNullOrEmpty(user.ExternalName))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var authMode = configuration.GetValue("Auth:Mode", AuthMode.Internal);
        if (authMode == AuthMode.Internal)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        var provider = schemes.FirstOrDefault();
        if (provider == null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dbContext.UserLogins.Add(new IdentityUserLogin<Guid>
        {
            UserId = user.Id,
            LoginProvider = provider.Name,
            ProviderKey = user.ExternalName,
            ProviderDisplayName = provider.DisplayName
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<HashSet<Guid>> GetGroupsWithChildren(Guid rootGroupId, CancellationToken cancellationToken)
    {
        var all = await dbContext.UsersGroups
            .AsNoTracking()
            .Select(g => new { g.Id, g.ParentId })
            .ToListAsync(cancellationToken);

        var childrenMap = all
            .GroupBy(x => x.ParentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootGroupId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!result.Add(current))
                continue;

            if (!childrenMap.TryGetValue(current, out var children))
                continue;

            foreach (var child in children)
                stack.Push(child);
        }

        return result;
    }

    private async Task<ActionResult?> ValidateGroupRequest(UpsertUsersGroupRequest request, Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не заполнено наименование группы");

        if (request.ParentId == Guid.Empty)
            return BadRequest("Некорректный идентификатор родительской группы");

        if (request.ParentId != null)
        {
            if (currentId != null && request.ParentId == currentId)
                return BadRequest("Группа не может быть родителем самой себя");

            var parentExists = await dbContext.UsersGroups
                .AsNoTracking()
                .AnyAsync(g => g.Id == request.ParentId, cancellationToken);

            if (!parentExists)
                return BadRequest("Указана несуществующая родительская группа");
        }

        return null;
    }

    private async Task<ActionResult?> ValidateAccountRequest(UpsertUserAccountRequest request, Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest("Не заполнено имя пользователя");

        var groupId = request.GroupId == Guid.Empty ? BuiltInDbData.EveryoneGroup.Id : request.GroupId;

        var groupExists = await dbContext.UsersGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == groupId, cancellationToken);
        if (!groupExists)
            return BadRequest("Не указана группа пользователей");

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var alreadyExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.NormalizedUserName == normalizedUserName && (currentId == null || u.Id != currentId),
                cancellationToken);

        if (alreadyExists)
            return BadRequest("Пользователь с таким логином уже существует");

        return null;
    }

    public sealed record UsersGroupListItem(
        Guid Id,
        string Name,
        Guid? ParentId,
        bool IsBuiltIn,
        int UsersCount);

    public sealed record UpsertUsersGroupRequest(
        string Name,
        Guid? ParentId);

    public sealed record UserAccountListItem(
        Guid Id,
        string UserName,
        string? DisplayName,
        string? ExternalName,
        Guid GroupId,
        string GroupName);

    public sealed record UpsertUserAccountRequest(
        string UserName,
        Guid GroupId,
        string? DisplayName,
        string? ExternalName,
        string? Password);
}
