using OnecMonitor.Common.DTO;
using OnecMonitor.Common.EventLog;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;

namespace OnecMonitor.Common.Services;

public class EventLogRepositoryManager
{
    public EventLogSettingsDto? Settings { get; private set; }

    public EventHandler<EventLogSettingsDto>? SettingsChanged;

    public void SetSettings(EventLogSettingsDto settings)
    {
        Settings = settings;
        SettingsChanged?.Invoke(this, Settings);
    }
    
    public IEventLogRepository GetInstance()
    {
        if (Settings == null)
            throw new Exception("Не установлены настройки хранилища журнала регистрации");
        
        if (Settings.Dbms.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");
        
        return new ClickHouseContext(Settings.Dbms, Settings.Credentials, Settings.DatabaseName, Settings.Table);
    }
}