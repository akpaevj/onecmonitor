using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагБлокировкиСоединений", "LockConnectionsStep")]
[MessagePackObject]
public class LockConnectionsStepDto
{
    [ContextProperty("КодДоступа", "AccessCode")]
    [Key(0)]
    public string AccessCode { get; set; }

    [ContextProperty("Сообщение", "Message")]
    [Key(1)]
    public string Message { get; set; }
}