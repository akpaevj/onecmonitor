using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using OneSwiss.Common.Models;
using OneSwiss.Common.Storage;

namespace OneSwiss.Common.Services;

public class EventLogRepositoryManager
{
    public EventLogSettingsDto? Settings { get; private set; }

    public EventHandler<EventLogSettingsDto>? SettingsChanged;

    public void UpdateSettings(EventLogSettingsDto settings)
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