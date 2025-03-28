using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.ViewModels.V8Files;

public class V8FileListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public V8FileType FileType { get; set; }

    public string GetPostfix()
        =>  FileType switch
        {
            V8FileType.Cf => "конфигурация",
            V8FileType.Cfe => "расширение конфигурации",
            V8FileType.Cfu => "обновление конфигурации",
            V8FileType.Epf => "внешняя обработка",
            _ => "неизвестный"
        };
    
    public override string ToString()
        => $"{Name} ({Version}, {GetPostfix()})";
}