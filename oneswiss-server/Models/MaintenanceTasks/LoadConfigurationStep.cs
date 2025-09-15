using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class LoadConfigurationStep
{
    public bool FromConfigRepository { get; set; }
    public bool LoadExactVersion { get; set; }
    public int Version { get; set; }
    public Guid? FileId { get; set; }
    public Guid? ConfigurationRepositoryId { get; set; }

    [ForeignKey(nameof(FileId))] public File? File { get; set; }

    [ForeignKey(nameof(ConfigurationRepositoryId))]
    public ConfigurationRepository? ConfigurationRepository { get; set; }
}