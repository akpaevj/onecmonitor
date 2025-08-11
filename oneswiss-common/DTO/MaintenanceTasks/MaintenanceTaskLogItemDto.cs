using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ЭлементЛогаЗадачиОбслуживания", "MaintenanceTaskLogItem")]
[MessagePackObject]
public class MaintenanceTaskLogItemDto
{
    [Key(0)] 
    [ContextProperty("Идентификатор", "Id")]
    public Guid Id { get; set; }
    [Key(1)] 
    [ContextProperty("Дата", "Date")]
    public DateTime TimeStamp { get; set; }
    [Key(2)] 
    [ContextProperty("ЭтоОшибка", "IsError")]
    public bool IsError { get; set; }
    [Key(3)]
    [ContextProperty("ЭтоФиниш", "IsFinish")]
    public bool IsFinish { get; set; }
    [Key(4)] 
    [ContextProperty("Сообщение", "Message")]
    public string Message { get; set; } = string.Empty;
    [Key(5)] 
    [ContextProperty("ИдентификаторИнформационнойБазы", "InfoBaseId")]
    public Guid? InfoBaseId { get; set; }
    [Key(6)] 
    [ContextProperty("ИдентификаторШага", "StepId")]
    public Guid? StepId { get; set; }
    [Key(7)] 
    [ContextProperty("ИдентификаторЗадачи", "TaskId")]
    public Guid TaskId { get; set; }
}