using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

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
    [DisplayName("Платформа")]
    public V8Platform Platform { get; set; } = null!;
}