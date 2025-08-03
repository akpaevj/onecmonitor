using System.ComponentModel.DataAnnotations.Schema;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class UpdateConfigurationStep : DatabaseObject
{
    public Guid? FileId { get; set; }
    
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
}