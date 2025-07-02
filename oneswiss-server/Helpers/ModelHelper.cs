using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Helpers;

public class ModelHelper
{
    public static void UpdateModelItems<T1>(
        IReadOnlyCollection<T1> newItems,
        List<T1> modelItems) where T1 : DatabaseObject
    {
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