using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class LoadExtensionStep : DatabaseObject
{
    public bool FromConfigRepository { get; set; }
    [MaxLength(200)]
    public string ExtensionName { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public Guid? ConfigurationRepositoryId { get; set; }
    
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository? ConfigurationRepository { get; set; }
}