using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class DeleteExtensionStepDto
{
    [Key(0)]
    public string ExtensionName { get; set; }
}