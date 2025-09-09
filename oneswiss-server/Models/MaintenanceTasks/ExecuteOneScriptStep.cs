using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OneSwiss.Server.Models.MaintenanceTasks;

[Owned]
public class ExecuteOneScriptStep
{
    public bool DebugMode { get; set; }

    [Required(AllowEmptyStrings = true)] public string ExecutablePath { get; set; } = string.Empty;

    public Guid? FileId { get; set; }

    [ForeignKey(nameof(FileId))] public File? File { get; set; }
}