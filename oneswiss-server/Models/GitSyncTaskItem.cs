using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models;

public class GitSyncTaskItem : DatabaseObject
{
    public Guid GitSyncTaskId { get; set; }

    [ForeignKey(nameof(GitSyncTaskId))] 
    public GitSyncTask GitSyncTask { get; set; }

    public bool IsActive { get; set; }
    public Guid ConfigurationRepositoryId { get; set; }

    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository ConfigurationRepository { get; set; }

    public bool IsExtension { get; set; }
    public Guid? BaseConfigurationRepositoryId { get; set; }

    [ForeignKey(nameof(BaseConfigurationRepositoryId))]
    public ConfigurationRepository? BaseConfigurationRepository { get; set; }
    public string ExportFolder { get; set; }
    public int ConfigurationRepositoryVersion { get; set; }
    public string LfsTrackers { get; set; }
}