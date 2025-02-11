namespace OnecMonitor.Server.ViewModels.Configurations;

public class ConfigurationListItemViewModel
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
            else
                return "Расширение";
        }
    }
}