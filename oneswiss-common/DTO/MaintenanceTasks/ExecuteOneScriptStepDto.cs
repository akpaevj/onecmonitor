using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO.MaintenanceTasks;

[ContextClass("ШагИсполненияОскрипта", "ExecuteOneScriptStep")]
[MessagePackObject]
public class ExecuteOneScriptStepDto
{
    [ContextProperty("Файл", "File")]
    [Key(0)]
    public FileDto File { get; set; }

    [ContextProperty("РежимОтладки", "DebugMode")]
    [Key(1)]
    public bool DebugMode { get; set; }

    [ContextProperty("ПутьИсполняемогоФайла", "ExecutableFilePath")]
    [Key(2)]
    public string ExecutablePath { get; set; }
}