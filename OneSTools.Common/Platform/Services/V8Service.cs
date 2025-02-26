using System.ComponentModel;
using MessagePack;

namespace OneSTools.Common.Platform.Services;

[DisplayName("Служба 1С")]
[MessagePackObject]
public class V8Service
{
    [DisplayName("Имя")]
    [Key(0)] 
    public string Name { get; set; }
    [DisplayName("Запущена")]
    [Key(1)] 
    public bool IsActive { get; set; }
}