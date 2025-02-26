using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OnecMonitor.Server.Models.MaintenanceTasks;

public enum MaintenanceStepKind
{
    [Display(Name = "Блокировка сеансов")]
    LockConnections,
    [Display(Name = "Закрытие сеансов")]
    CloseConnections,
    [Display(Name = "Разблокировка сеансов")]
    UnlockConnections,
    [Display(Name = "Загрузка расширения")]
    LoadExtension,
    [Display(Name = "Обновление конфигурации")]
    UpdateConfiguration,
    [Display(Name = "Загрузка конфигурации")]
    LoadConfiguration,
    [Display(Name = "Обновление информационной базы")]
    UpdateDatabase,
    [Display(Name = "Запуск внешней обработки")]
    StartExternalDataProcessor
}