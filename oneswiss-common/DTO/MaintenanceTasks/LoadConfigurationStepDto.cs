using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагЗагрузкиКонфигурации", "LoadConfigurationStep")]
[MessagePackObject]
public class LoadConfigurationStepDto
{
    [ContextProperty("ИзХранилищаКонфигурации", "FromConfigRepository")]
    [Key(0)]
    public bool FromConfigRepository { get; set; }
    [ContextProperty("Файл", "File")]
    [Key(1)]
    public FileDto? File { get; set; }
    [ContextProperty("ХранилищеКонфигурации", "ConfigurationRepository")]
    [Key(2)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}