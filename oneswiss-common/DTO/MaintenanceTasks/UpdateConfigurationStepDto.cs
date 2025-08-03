using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class UpdateConfigurationStepDto
{
    [Key(0)]
    public FileDto File { get; set; }
}