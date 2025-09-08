using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("ЭлементЗадачиСинхронизации", "GitSyncTaskItem")]
[MessagePackObject]
public class GitSyncTaskItemDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; }

    [ContextProperty("ХранилищеКонфигурации", "ConfigurationRepository")]
    [Key(1)]
    public ConfigurationRepositoryDto ConfigurationRepository { get; set; }

    [ContextProperty("Активна", "IsActive")]
    [Key(2)]
    public bool IsActive { get; set; }
    
    [ContextProperty("ЭтоРасширение", "IsExtension")]
    [Key(3)]
    public bool IsExtension { get; set; }

    [ContextProperty("Имя", "Name")]
    [Key(5)]
    public string ExportFolder { get; set; }

    [ContextProperty("ВерсияХранилищаКонфигурации", "ConfigurationRepositoryVersion")]
    [Key(6)]
    public int ConfigurationRepositoryVersion { get; set; }

    [ContextProperty("ТрекерыLFS", "LfsTrackers")]
    [Key(7)]
    public string LfsTrackers { get; set; }
}