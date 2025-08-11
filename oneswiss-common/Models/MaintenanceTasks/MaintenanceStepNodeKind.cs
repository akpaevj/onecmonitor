using System.ComponentModel.DataAnnotations;
using OneScript.Contexts.Enums;

namespace OneSwiss.Common.Models.MaintenanceTasks;

[EnumerationType("ТипНодыШагаОбслуживания", "MaintenanceStepNodeKind")]
public enum MaintenanceStepNodeKind
{
    [EnumValue("Простая", "Simple")]
    [Display(Name = "Простой")]
    Simple,
    [EnumValue("Попытка", "TryCatch")]
    [Display(Name = "Попытка/Исключение")]
    TryCatch
}