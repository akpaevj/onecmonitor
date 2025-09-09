using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагЗагрузкиРасширения", "LoadExtensionStep")]
[MessagePackObject]
public class LoadExtensionStepDto
{
    [ContextProperty("ИмяРасширения", "ExtensionName")]
    [Key(0)]
    public string ExtensionName { get; set; }

    [ContextProperty("ИзХранилищаКонфигурации", "FromConfigRepository")]
    [Key(1)]
    public bool FromConfigRepository { get; set; }

    [ContextProperty("Файл", "File")]
    [Key(2)]
    public FileDto? File { get; set; }

    [ContextProperty("ХранилищеКонфигурации", "ConfigurationRepository")]
    [Key(3)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}