namespace OneSwiss.Agent.Services.GitSync;

public class GitSyncTaskItemMetadata
{
    public Guid ConfigurationRepositoryId { get; set; } = Guid.Empty;
    public string ExportFolder { get; set; } = string.Empty;
    public int Version { get; set; } = 0;
    public bool IsExtension { get; set; }
}