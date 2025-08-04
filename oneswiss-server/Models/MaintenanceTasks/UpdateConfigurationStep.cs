using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class UpdateConfigurationStep
{
    public Guid? FileId { get; set; }
    [ForeignKey(nameof(FileId))]
    public File? File { get; set; }
}