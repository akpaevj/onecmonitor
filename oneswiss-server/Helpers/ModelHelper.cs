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
}