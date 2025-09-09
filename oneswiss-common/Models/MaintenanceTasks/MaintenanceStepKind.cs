using System.ComponentModel.DataAnnotations;
using OneScript.Contexts.Enums;

namespace OneSwiss.Common.Models.MaintenanceTasks;

[EnumerationType("ТипШагаОбслуживания", "MaintenanceStepKind")]
public enum MaintenanceStepKind
{
    [EnumValue("БлокировкаСоединений", "LockConnections")] [Display(Name = "Блокировка соединений")]
    LockConnections,

    [EnumValue("ЗакрытиеСеансов", "CloseConnections")] [Display(Name = "Закрытие сеансов")]
    CloseConnections,

    [EnumValue("РазблокировкаСоединений", "UnlockConnections")] [Display(Name = "Разблокировка соединений")]
    UnlockConnections,

    [EnumValue("ЗагрузкаРасширения", "LoadExtension")] [Display(Name = "Загрузка расширения")]
    LoadExtension,

    [EnumValue("УдалениеРасширения", "DeleteExtension")] [Display(Name = "Удаление расширения")]
    DeleteExtension,

    [EnumValue("ОбновлениеКонфигурации", "UpdateConfiguration")] [Display(Name = "Обновление конфигурации")]
    UpdateConfiguration,

    [EnumValue("ЗагрузкаКонфигурации", "LoadConfiguration")] [Display(Name = "Загрузка конфигурации")]
    LoadConfiguration,

    [EnumValue("ЗапускВнешнейОбработки", "StartExternalDataProcessor")] [Display(Name = "Запуск внешней обработки")]
    StartExternalDataProcessor,

    [EnumValue("ВыполнениеОСкрипта", "ExecuteOneScript")] [Display(Name = "Выполнение скрипта - OneScript")]
    ExecuteOneScript,

    [EnumValue("КопированиеИнформационнойБазы", "CopyInfoBase")] [Display(Name = "Копирование информационной базы")]
    CopyInfoBase
}