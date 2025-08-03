using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Common.Models.MaintenanceTasks;

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
    [Display(Name = "Удаление расширения")]
    DeleteExtension,
    [Display(Name = "Обновление конфигурации")]
    UpdateConfiguration,
    [Display(Name = "Загрузка конфигурации")]
    LoadConfiguration,
    [Display(Name = "Запуск внешней обработки")]
    StartExternalDataProcessor,
    [Display(Name = "Выполнение скрипта - OneScript")]
    ExecuteOneScript,
    [Display(Name = "Копирование информационной базы")]
    CopyInfoBase
}