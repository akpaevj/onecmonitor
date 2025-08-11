using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагУдаленияРасширения", "DeleteExtensionStep")]
[MessagePackObject]
public class DeleteExtensionStepDto
{
    [ContextProperty("ИмяРасширения", "ExtensionName")]
    [Key(0)]
    public string ExtensionName { get; set; }
}