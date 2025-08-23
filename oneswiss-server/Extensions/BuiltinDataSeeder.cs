using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.Extensions;

public static class BuiltinDataSeeder
{
    public static async Task SeedBuiltInData(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.SeedRoles();
        await scope.ServiceProvider.SeedAccessGroups();
        await scope.ServiceProvider.SeedUsersGroups();
        await scope.ServiceProvider.SeedUsers();
    }
    
    private static async Task SeedUsers(this IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await manager.Users.AnyAsync())
            return;
        
        await manager.CreateAsync(new ApplicationUser
        {
            UserName = BuiltInDbData.AdminUser.User,
            DisplayName = BuiltInDbData.AdminUser.DisplayName,
            GroupId = BuiltInDbData.AdminsGroup.Id,
        }, BuiltInDbData.AdminUser.Password);
    }

    private static async Task SeedRoles(this IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in BuiltInRoles.Roles)
            if (!await manager.RoleExistsAsync(role.Name))
                await manager.CreateAsync(new ApplicationRole(role.Name, role.Description));
    }

    private static async Task SeedAccessGroups(this IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<AccessGroupsManager>();
        var rolesManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (await manager.GroupsExists())
            return;

        await manager.Create(new AccessGroup
        {
            Id = BuiltInDbData.AdminsAccessGroup.Id,
            IsBuiltIn = true,
            Name = BuiltInDbData.AdminsAccessGroup.Name,
            Roles = [rolesManager.Roles.First(c => c.Name == "Administrator")]
        });
    }

    private static async Task SeedUsersGroups(this IServiceProvider serviceProvider)
    {
        var manager = serviceProvider.GetRequiredService<UserGroupsManager>();

        if (await manager.GroupsExists())
            return;

        var everyOneGroup = new UsersGroup
        {
            Id = BuiltInDbData.EveryoneGroup.Id,
            IsBuiltIn = true,
            Name = BuiltInDbData.EveryoneGroup.Name
        };
        await manager.Create(everyOneGroup);

        var adminsGroup = new UsersGroup
        {
            Id = BuiltInDbData.AdminsGroup.Id,
            IsBuiltIn = true,
            Name = BuiltInDbData.AdminsGroup.Name,
            ParentId = BuiltInDbData.EveryoneGroup.Id
        };
        await manager.Create(adminsGroup, [BuiltInDbData.AdminsAccessGroup.Id]);
    }
}