using OneSwiss.Common.Models;

namespace OneSwiss.Server.Models;

public class NotificationRecipient : DatabaseObject
{
    public NotificationChannel Channel { get; set; }
    public string SendTo { get; set; }
    public List<NotificationType> NotificationTypes { get; set; }
    public List<CustomNotification> CustomNotifications { get; set; }
}