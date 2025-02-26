namespace OnecMonitor.Server.ViewModels.V8Files;

public class V8FileListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsUpdate { get; set; }
    public bool IsConfiguration { get; set; }
    public bool IsExtension { get; set; }

    public string Type
    {
        get
        {
            if (IsUpdate)
                return "Обновление";
            else if (IsConfiguration)
                return "Конфигурация";
            else if (IsExtension)
                return "Расширение";
            else
                return "Внешняя обработка";
        }
    }
}