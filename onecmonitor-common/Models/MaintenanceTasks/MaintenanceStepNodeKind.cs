using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Common.Models.MaintenanceTasks;

public enum MaintenanceStepNodeKind
{
    [Display(Name = "Простой")]
    Simple,
    [Display(Name = "Попытка/Исключение")]
    TryCatch
}