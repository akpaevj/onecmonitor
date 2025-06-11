namespace OnecMonitor.Server.Models;

public class V8File : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DataPath { get; set; }  = string.Empty;
    public bool IsArchived { get; set; }
    public V8FileType FileType { get; set; }

    public override string ToString()
    {
        var postfix = FileType switch
        {
            V8FileType.Cf => "конфигурация",
            V8FileType.Cfe => "расширение",
            V8FileType.Cfu => "обновление",
            V8FileType.Epf => "внешняя обработка",
            V8FileType.Ospx => "скрипт (OneScript)",
            _ => "неизвестный"
        };
        
        return $"{Name} ({Version}, {postfix})";
    }
}