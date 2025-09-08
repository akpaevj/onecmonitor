using System.ComponentModel;
using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Platform.Services;

[ContextClass("СлужбаCrServer", "CrServerService")]
[DisplayName("Служба хранилища конфигураций 1С")]
public class CrServer : V8Service
{
    [Key(2)]
    [DisplayName("Порт")]
    [ContextProperty("Порт", "Port", CanWrite = false)]
    public int Port { get; set; }

    [Key(3)]
    [DisplayName("Каталог")]
    [ContextProperty("Каталог", "Directory", CanWrite = false)]
    public string Directory { get; set; } = null!;

    [Key(4)]
    [DisplayName("Платформа")]
    [ContextProperty("Платформа", "Platform", CanWrite = false)]
    public V8Platform Platform { get; set; } = null!;

    [Key(5)]
    [DisplayName("Список хранилищ")]
    public List<string> Repositories { get; set; } = [];
}