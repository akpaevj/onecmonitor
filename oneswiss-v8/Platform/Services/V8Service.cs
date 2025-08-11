using System.ComponentModel;
using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.V8.Platform.Services;

[DisplayName("Служба 1С")]
[ContextClass("СлужбаV8", "V8Service")]
[MessagePackObject]
public class V8Service
{
    [DisplayName("Имя")]
    [ContextProperty("Имя", "Name", CanWrite = false)]
    [Key(0)] 
    public string Name { get; set; } = string.Empty;
    [DisplayName("Запущена")]
    [ContextProperty("Запущена", "IsActive", CanWrite = false)]
    [Key(1)] 
    public bool IsActive { get; set; }
}