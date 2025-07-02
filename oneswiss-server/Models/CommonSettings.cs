namespace OneSwiss.Server.Models;

public class CommonSettings : DatabaseObject
{
    public string TelegramBotToken { get; set; } = string.Empty;
    public bool NotifyMaintenanceTaskCompleted { get; set; } = false;
    public bool NotifyMaintenanceTaskInfoBaseCompleted { get; set; } = false;
}