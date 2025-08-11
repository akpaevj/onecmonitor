using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагОбновленияКонфигурации", "UpdateConfigurationStep")]
[MessagePackObject]
public class UpdateConfigurationStepDto
{
    [ContextProperty("Файл", "File")]
    [Key(0)]
    public FileDto File { get; set; }
}