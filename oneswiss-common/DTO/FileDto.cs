using MessagePack;
using OneScript.Contexts;
using OneSwiss.OneScript.Oscript;

namespace OneSwiss.Common.DTO;

[ContextClass("ФайлOneSwiss", "OneSwissFile")]
[MessagePackObject]
public class FileDto
{
    [ContextProperty("Идентификатор", "Id", Converter = typeof(GuidContextConverter))]
    [Key(0)]
    public Guid Id { get; set; }
    [ContextProperty("Имя", "Name")]
    [Key(1)]
    public string Name { get; set; }
    [ContextProperty("Расширение", "Extension")]
    [Key(2)]
    public string FileExtension { get; set; }
    [ContextProperty("Версия", "Version")]
    [Key(3)]
    public string Version { get; set; }
    [ContextProperty("ЭтоОбновление", "IsUpdate")]
    [Key(4)] 
    public bool IsUpdate { get; set; } = false;
    [ContextProperty("ЭтоРасширение", "IsExtension")]
    [Key(5)] 
    public bool IsExtension { get; set; } = false;
    [ContextProperty("ЭтоКонфигурация", "IsConfiguration")]
    [Key(6)] 
    public bool IsConfiguration { get; set; } = false;
    [ContextProperty("Размер", "Length")]
    [Key(7)] 
    public long Length { get; set; }
}