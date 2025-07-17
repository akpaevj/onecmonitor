using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public class Notification : DatabaseObject
{
    public NotificationType Type  { get; set; }
    public DateTime CreatedAt  { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Recipient  { get; set; } = string.Empty;
    public string Additionalinfo { get; set; } = string.Empty;
}