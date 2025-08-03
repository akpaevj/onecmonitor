using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class LoadConfigurationStepDto
{
    [Key(0)]
    public bool FromConfigRepository { get; set; }
    [Key(1)]
    public FileDto? File { get; set; }
    [Key(2)]
    public ConfigurationRepositoryDto? ConfigurationRepository { get; set; }
}