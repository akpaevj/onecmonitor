using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

[DisplayName("Служба сервера удаленного администрирования 1С")]
[MessagePackObject]
public class RasService : V8Service
{
    [Key(2)] 
    [DisplayName("Хост агента кластера")] 
    public string RagentHost { get; set; } = string.Empty;
    [Key(3)] 
    [DisplayName("Порт агента кластера")]
    public int RagentPort { get; set; }
    [Key(4)] 
    [DisplayName("Порт")]
    public int Port { get; set; }
    [Key(5)]
    [DisplayName("Платформа")]
    public V8Platform Platform { get; set; } = null!;
}