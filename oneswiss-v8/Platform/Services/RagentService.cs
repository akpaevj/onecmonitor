using System.ComponentModel;
using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба агента сервера 1С")]
[MessagePackObject]
[ContextClass("СервисRagent", "RagentService")]
public class RagentService : V8Service
{
    [Key(2)]
    [DisplayName("Порт")]
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }

    [Key(3)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;

    [Key(4)]
    [DisplayName("Каталог агента сервера")]
    [ContextProperty("РабочийКаталог", "WorkingDirectory", CanWrite = false)]
    public string WorkingDirectory { get; set; } = null!;

    [Key(5)]
    [DisplayName("Тип отладки")]
    [ContextProperty("ТипОтладки", "DebugType", CanWrite = false)]
    public RagentDebugType DebugType { get; set; } = RagentDebugType.None;
}