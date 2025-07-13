using System.ComponentModel;
using MessagePack;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба агента сервера 1С")]
[MessagePackObject]
public class RagentService : V8Service
{
    [Key(2)] 
    [DisplayName("Порт")]
    public int Port { get; set; }
    [Key(3)] 
    [DisplayName("Порт кластера")]
    public int RegPort { get; set; }
    [Key(4)]
    [DisplayName("Каталог кластера")]
    public string ClusterCatalog { get; set; } = null!;
    [Key(5)]
    [DisplayName("Платформа")]
    public V8Platform Platform { get; set; } = null!;
    [Key(6)] [DisplayName("Тип отладки")] 
    public RagentDebugType DebugType { get; set; } = RagentDebugType.None;
}