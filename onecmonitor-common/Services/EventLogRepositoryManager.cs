using OnecMonitor.Common.DTO;
using OnecMonitor.Common.EventLog;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;

namespace OnecMonitor.Common.Services;

public class EventLogRepositoryManager
{
    private EventLogSettingsDto? _settings;

    public EventHandler<EventLogSettingsDto>? SettingsChanged;

    public void SetSettings(EventLogSettingsDto settings)
    {
        _settings = settings;
        SettingsChanged?.Invoke(this, _settings);
    }
    
    public IEventLogRepository GetInstance()
    {
        if (_settings == null)
            throw new Exception("Не установлены настройки хранилища журнала регистрации");
        
        if (_settings.Dbms.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");
        
        return new ClickHouseContext(_settings.Dbms, _settings.Credentials, _settings.DatabaseName, _settings.Table);
    }
}