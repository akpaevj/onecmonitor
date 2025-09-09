using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services;

public class UserGroupsManager(IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> contextFactory)
{
    private async Task<List<ApplicationRole>> GetGroupRoles(Guid groupId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var group = await context.UsersGroups
            .AsNoTracking()
            .Include(c => c.AccessGroups)
            .ThenInclude(c => c.Roles)
            .FirstAsync(c => c.Id == groupId);

        var result = new List<ApplicationRole>(group.AccessGroups.SelectMany(c => c.Roles));

        if (group.ParentId != null)
            result.AddRange(await GetGroupRoles((Guid)group.ParentId));

        return result;
    }

    public async Task UpdateUsersRoles()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = await contextFactory.CreateDbContextAsync();
        using var usersManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await UpdateGroupUsersRoles(context, usersManager, BuiltInDbData.EveryoneGroup.Id);
    }

    private async Task UpdateGroupUsersRoles(AppDbContext context, UserManager<ApplicationUser> usersManager,
        Guid groupId)
    {
        var group = await context.UsersGroups
            .Include(usersGroup => usersGroup.Users)
            .Include(usersGroup => usersGroup.Groups)
            .FirstAsync(c => c.Id == groupId);
        var groupRoles = await GetGroupRoles(groupId);
        var groupRolesNames = groupRoles.Select(c => c.NormalizedName).ToList();

        foreach (var applicationUser in group.Users)
        {
            var user = await usersManager.Users.FirstOrDefaultAsync(c => c.Id == applicationUser.Id);
            var userRoles = await usersManager.GetRolesAsync(user!);
            var newRoles = groupRoles
                .Where(c => !userRoles.Contains(c.NormalizedName, StringComparer.InvariantCultureIgnoreCase)).ToList();
            var removedRoles = userRoles
                .Where(c => !groupRolesNames.Contains(c, StringComparer.InvariantCultureIgnoreCase)).ToList();

            await usersManager.AddToRolesAsync(user!, newRoles.Select(c => c.Name!));
            await usersManager.RemoveFromRolesAsync(user!, removedRoles);
        }

        foreach (var childGroup in group.Groups)
            await UpdateGroupUsersRoles(context, usersManager, childGroup.Id);
    }

    public async Task<bool> GroupsExists()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.UsersGroups.AnyAsync();
    }

    public async Task Create(UsersGroup group)
    {
        await Create(group, []);
    }

    public async Task Create(UsersGroup group, Guid[] accessGroupsIds)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var accessGroups = context.AccessGroups.Where(c => accessGroupsIds.Contains(c.Id)).ToList();
        group.AccessGroups.AddRange(accessGroups);

        await context.UsersGroups.AddAsync(group);
        await context.SaveChangesAsync();
    }

    public async Task AddUserToGroup(ApplicationUser user, Guid groupId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        using var usersManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var context = await contextFactory.CreateDbContextAsync();

        var group = context.UsersGroups
            .Include(c => c.Users)
            .Include(c => c.AccessGroups).ThenInclude(c => c.Roles)
            .First(c => c.Id == groupId);

        foreach (var identityRole in group.AccessGroups.SelectMany(accessGroup => accessGroup.Roles))
            await usersManager.AddToRoleAsync(user, identityRole.Name!);

        group.Users.Add(user);

        await context.SaveChangesAsync();
    }
}