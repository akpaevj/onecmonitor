using OneSwiss.Common.DTO.MaintenanceTasks;

namespace OneSwiss.Agent.Extensions;

public static class MaintenanceStepsListExtension
{
    public static MaintenanceStepDto GetRootStep(this List<MaintenanceStepDto> items)
        => items.SingleOrDefault(s => s.PreviousStepId is null)!;
    
    public static MaintenanceStepDto GetStep(this List<MaintenanceStepDto> items, Guid? stepId)
        => items.SingleOrDefault(s => s.Id == stepId)!;
}