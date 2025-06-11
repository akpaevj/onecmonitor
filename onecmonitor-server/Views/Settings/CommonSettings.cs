using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Views.Settings;

public class CommonSettings : DatabaseObject
{
    public string TelegramBotToken { get; set; } = string.Empty;
    public bool NotifyMaintenanceTaskCompleted { get; set; } = false;
    public bool NotifyMaintenanceTaskInfoBaseCompleted { get; set; } = false;
}