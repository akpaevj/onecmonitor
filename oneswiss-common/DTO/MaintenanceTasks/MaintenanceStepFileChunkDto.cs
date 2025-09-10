using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class MaintenanceStepFileChunkDto
{
    [Key(0)] public Guid Id { get; set; }

    [Key(1)] public byte[] Data { get; set; } = [];
}