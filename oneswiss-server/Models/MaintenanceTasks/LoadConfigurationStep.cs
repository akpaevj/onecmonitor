using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class LoadConfigurationStep : DatabaseObject
{
    public bool FromConfigRepository { get; set; }
    public Guid? FileId { get; set; }
    public Guid? ConfigurationRepositoryId { get; set; }
    
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository? ConfigurationRepository { get; set; }
}