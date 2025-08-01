using System.ComponentModel;
using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба сервера удаленного администрирования 1С")]
[MessagePackObject]
[ContextClass("СервисRas", "RasService")]
public class RasService : V8Service
{
    [Key(2)] 
    [DisplayName("Хост агента кластера")]
    [ContextProperty("ХостRagent", "RagentHost", CanWrite = false)]
    public string RagentHost { get; set; } = string.Empty;
    [Key(3)] 
    [DisplayName("Порт агента кластера")]
    [ContextProperty("ПортRagent", "RagentPort", CanWrite = false)]
    public int RagentPort { get; set; }
    [Key(4)] 
    [DisplayName("Порт")]
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }
    [Key(5)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;
}