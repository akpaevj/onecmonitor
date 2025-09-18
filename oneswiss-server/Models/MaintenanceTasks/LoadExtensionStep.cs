using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class LoadExtensionStep
{
    public bool FromConfigRepository { get; set; }
    public bool LoadExactVersion { get; set; }
    public int Version { get; set; }

    [MaxLength(200)] 
    public string ExtensionName { get; set; } = string.Empty;

    public Guid? FileId { get; set; }
    public Guid? BaseConfigurationRepositoryId { get; set; }
    public Guid? ConfigurationRepositoryId { get; set; }

    [ForeignKey(nameof(FileId))] 
    public File? File { get; set; }

    [ForeignKey(nameof(BaseConfigurationRepositoryId))]
    public ConfigurationRepository? BaseConfigurationRepository { get; set; }
    
    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository? ConfigurationRepository { get; set; }
}