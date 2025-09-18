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
    
    [ContextProperty("ЗагружатьКонкретнуюВерсию", "LoadExactVersion")]
    [Key(2)]
    public bool LoadExactVersion { get; set; }
    
    [ContextProperty("Версия", "Version")]
    [Key(3)]
    public int Version { get; set; }

    [ContextProperty("Файл", "File")]
    [Key(4)]
    public FileDto? File { get; set; }
    
    [ContextProperty("ХранилищеБазовойКонфигурации", "BaseConfigurationRepository")]
    [Key(5)]
    public ConfigurationRepositoryDto? BaseConfigurationRepository { get; set; }

    [ContextProperty("ХранилищеКонфигурации", "ConfigurationRepository")]
    [Key(6)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}