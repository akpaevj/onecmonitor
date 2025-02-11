using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform;

[DisplayName("Служба 1С")]
[MessagePackObject]
public class V8Service
{
    [DisplayName("Тип")]
    [Key(0)] 
    public V8ServiceType Type { get; set; }
    [DisplayName("Имя")]
    [Key(1)] 
    public string Name { get; set; }
    [DisplayName("Запущена")]
    [Key(2)] 
    public bool IsActive { get; set; }
    [Key(3)] 
    [DisplayName("Порт")]
    public int Port { get; set; }
    [DisplayName("Путь к платформе")]
    [Key(4)] 
    public string PlatformPath { get; set; }
}