using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public enum NotificationType
{
    [Display(Name = "Завершение задачи обслуживания")]
    MaintenanceTaskCompleted,
    [Display(Name = "Получен отчет об ошибке")]
    ErrorReportReceived,
    [Display(Name = "Пользовательское уведомление")]
    Custom
}