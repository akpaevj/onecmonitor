using MessagePack;

namespace OneSwiss.Common.DTO;

[MessagePackObject]
public class GitSyncTasksProcessorStopped
{
    [Key(0)] public Guid TaskId { get; set; }

    [Key(1)] public Guid ConfigurationRepositoryId { get; set; }

    [Key(2)] public string Reason { get; set; } = string.Empty;
}