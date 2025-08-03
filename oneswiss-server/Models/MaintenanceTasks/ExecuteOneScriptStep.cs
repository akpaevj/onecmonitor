using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models.MaintenanceTasks;

public class ExecuteOneScriptStep : DatabaseObject
{
    public bool DebugMode { get; set; }
    [Required(AllowEmptyStrings = true)]
    public string ExecutablePath { get; set; } = string.Empty;
    public Guid? FileId { get; set; }
    public File? File { get; set; }
}