using System.ComponentModel;
using MessagePack;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба 1С")]
[MessagePackObject]
public class V8Service
{
    [DisplayName("Имя")]
    [Key(0)] 
    public string Name { get; set; } = string.Empty;
    [DisplayName("Запущена")]
    [Key(1)] 
    public bool IsActive { get; set; }
}