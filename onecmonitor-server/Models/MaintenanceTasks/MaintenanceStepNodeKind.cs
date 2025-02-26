using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public enum MaintenanceStepNodeKind
{
    [Display(Name = "Простой")]
    Simple,
    [Display(Name = "Попытка/Исключение")]
    TryCatch
}