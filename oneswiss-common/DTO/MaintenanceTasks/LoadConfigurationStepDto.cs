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
    
    [ContextProperty("ЗагружатьКонкретнуюВерсию", "LoadExactVersion")]
    [Key(1)]
    public bool LoadExactVersion { get; set; }
    
    [ContextProperty("Версия", "Version")]
    [Key(2)]
    public int Version { get; set; }

    [ContextProperty("Файл", "File")]
    [Key(3)]
    public FileDto? File { get; set; }

    [ContextProperty("ХранилищеКонфигурации", "ConfigurationRepository")]
    [Key(4)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}