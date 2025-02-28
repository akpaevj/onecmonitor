using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Common.Models.MaintenanceTasks;

public enum MaintenanceStepKind
{
    [Display(Name = "Блокировка соединений")]
    LockConnections,
    [Display(Name = "Закрытие сеансов")]
    CloseConnections,
    [Display(Name = "Разблокировка соединений")]
    UnlockConnections,
    [Display(Name = "Загрузка расширения")]
    LoadExtension,
    [Display(Name = "Обновление конфигурации")]
    UpdateConfiguration,
    [Display(Name = "Загрузка конфигурации")]
    LoadConfiguration,
    [Display(Name = "Запуск внешней обработки")]
    StartExternalDataProcessor
}