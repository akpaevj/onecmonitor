using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;

namespace OnecMonitor.Common.Services;

public class TechLogRepositoryManager
{
    public TechLogSettingsDto? Settings { get; private set; }

    public EventHandler<TechLogSettingsDto>? SettingsChanged;

    public void SetSettings(TechLogSettingsDto settings)
    {
        Settings = settings;
        SettingsChanged?.Invoke(this, Settings);
    }
    
    public ITechLogRepository GetInstance()
    {
        if (Settings == null)
            throw new Exception("Не установлены настройки хранилища технологического журнала");
        
        if (Settings.Dbms.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");
        
        return new ClickHouseContext(Settings.Dbms, Settings.Credentials, Settings.DatabaseName, Settings.Table);
    }
}