using System.ComponentModel;
using MessagePack;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба хранилища конфигураций 1С")]
public class CrServer : V8Service
{
    [Key(2)] 
    [DisplayName("Порт")]
    public int Port { get; set; }
    
    [Key(3)]
    [DisplayName("Каталог")]
    public string Directory { get; set; } = null!;
    
    [Key(4)]
    [DisplayName("Платформа")]
    public V8Platform Platform { get; set; } = null!;

    [Key(5)] 
    [DisplayName("Список хранилищ")] 
    public List<string> Reporitories { get; set; } = [];
}