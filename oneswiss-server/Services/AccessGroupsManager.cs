using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services;

public class AccessGroupsManager(AppDbContext context) : IDisposable, IAsyncDisposable
{
    public async Task<bool> GroupsExists()
        => await context.AccessGroups.AnyAsync();
    
    public async Task Create(AccessGroup group)
    {
        await context.AccessGroups.AddAsync(group);
        await context.SaveChangesAsync();
    }

    public async Task<AccessGroup?> GetById(Guid id)
        => await context.AccessGroups.FirstOrDefaultAsync(c => c.Id == id);
    
    public void Dispose()
    {
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}