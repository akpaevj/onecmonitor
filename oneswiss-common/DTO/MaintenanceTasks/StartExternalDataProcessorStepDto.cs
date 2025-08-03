using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class StartExternalDataProcessorStepDto
{
    [Key(0)]
    public FileDto File { get; set; }
}