using MessagePack;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[MessagePackObject]
public class ExecuteOneScriptStepDto
{
    [Key(0)]
    public FileDto File { get; set; }
    [Key(1)]
    public bool DebugMode { get; set; }
    [Key(2)]
    public string ExecutablePath { get; set; }
}