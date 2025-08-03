using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class LoadExtensionStepDto
{
    [Key(0)]
    public string ExtensionName { get; set; }
    [Key(1)]
    public bool FromConfigRepository { get; set; }
    [Key(2)]
    public FileDto? File { get; set; }
    [Key(3)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}