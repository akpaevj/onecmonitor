using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Common.Models.MaintenanceTasks;

public enum MaintenanceStepNodeKind
{
    [Display(Name = "Простой")]
    Simple,
    [Display(Name = "Попытка/Исключение")]
    TryCatch
}