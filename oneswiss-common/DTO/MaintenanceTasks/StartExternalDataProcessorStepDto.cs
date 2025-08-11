using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагЗапускаВнешнейОбработки", "StartExternalDataProcessorStep")]
[MessagePackObject]
public class StartExternalDataProcessorStepDto
{
    [ContextProperty("Файл", "File")]
    [Key(0)]
    public FileDto File { get; set; }
}