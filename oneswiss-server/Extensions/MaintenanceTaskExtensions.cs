using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models.MaintenanceTasks;

namespace OneSwiss.Server.Extensions;

public static class MaintenanceTaskExtensions
{
    public static IQueryable<MaintenanceTask> IncludeSteps(this IQueryable<MaintenanceTask> set)
        => set
            .Include(c => c.Steps).ThenInclude(c => c.CopyInfoBaseStep)
            .Include(c => c.Steps).ThenInclude(c => c.LoadExtensionStep)
            .Include(c => c.Steps).ThenInclude(c => c.LoadConfigurationStep)
            .Include(c => c.Steps).ThenInclude(c => c.UpdateConfigurationStep)
            .Include(c => c.Steps).ThenInclude(c => c.DeleteExtensionStep)
            .Include(c => c.Steps).ThenInclude(c => c.ExecuteOneScriptStep)
            .Include(c => c.Steps).ThenInclude(c => c.LockConnectionsStep)
            .Include(c => c.Steps).ThenInclude(c => c.StartExternalDataProcessorStep);
}