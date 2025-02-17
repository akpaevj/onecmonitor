using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Helpers;

public class ModelHelper
{
    public static async Task UpdateModelItems<T1>(
        IQueryable<T1> queryable,
        List<T1> modelItems,
        CancellationToken cancellationToken) where T1 : DatabaseObject
    {
        var ids = modelItems.Select(c => c.Id);
        
        var newItems = await queryable
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        // add new
        newItems
            .Where(c => !modelItems.Contains(c))
            .ToList()
            .ForEach(modelItems.Add);
        
        // remove deleted
        modelItems
            .Where(c => !newItems.Contains(c))
            .ToList()
            .ForEach(c => modelItems.Remove(c));
    }
}