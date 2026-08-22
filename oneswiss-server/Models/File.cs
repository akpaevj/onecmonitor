
namespace OneSwiss.Server.Models;

public class File : DatabaseObject
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string DataPath { get; set; } = string.Empty;

    public FileType FileType { get; set; }

    public override string ToString()
    {
        var postfix = FileType switch
        {
            FileType.Cf => "конфигурация",
            FileType.Cfe => "расширение",
            FileType.Cfu => "обновление",
            FileType.Epf => "внешняя обработка",
            FileType.Ospx => "скрипт (OneScript)",
            _ => "неизвестный"
        };

        return $"{Name} ({Version}, {postfix})";
    }
}