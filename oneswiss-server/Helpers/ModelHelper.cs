using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Helpers;

public class ModelHelper
{
    public static void UpdateModelItems<T>(
        IReadOnlyCollection<T> newItems,
        List<T> modelItems)
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
    
    public static void UpdateDbSet<T>(
        IReadOnlyCollection<T> newItems,
        DbContext context) where T : DatabaseObject
    {
        var dbSet = context.Set<T>();
        
        // add new
        newItems
            .Where(c => !dbSet.Contains(c))
            .ToList()
            .ForEach(i => dbSet.Add(i));
        
        // remove deleted
        dbSet
            .Where(c => !newItems.Contains(c))
            .ToList()
            .ForEach(c => dbSet.Remove(c));
        
        newItems
            .Where(c => dbSet.Contains(c))
            .ToList()
            .ForEach(c => context.Entry(c) .State = EntityState.Modified);
    }
}