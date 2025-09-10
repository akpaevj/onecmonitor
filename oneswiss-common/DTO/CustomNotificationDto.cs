using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
[ContextClass("Уведомление", "Notification")]
public class CustomNotificationDto
{
    [Key(0)]
    [ContextProperty("Ключ", "Key")]
    public string Key { get; set; }

    [Key(1)]
    [ContextProperty("Сообщение", "Message")]
    public string Message { get; set; }
}