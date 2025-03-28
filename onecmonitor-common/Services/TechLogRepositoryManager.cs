using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Models;
using OnecMonitor.Common.Storage;
using OnecMonitor.Common.TechLog;

namespace OnecMonitor.Common.Services;

public class TechLogRepositoryManager
{
    private TechLogSettingsDto? _settings;

    public TechLogSettingsDto? Settings => _settings;
    public EventHandler<TechLogSettingsDto>? SettingsChanged;

    public void SetSettings(TechLogSettingsDto settings)
    {
        _settings = settings;
        SettingsChanged?.Invoke(this, _settings);
    }
    
    public ITechLogRepository GetInstance()
    {
        if (_settings == null)
            throw new Exception("Не установлены настройки хранилища технологического журнала");
        
        if (_settings.Dbms.Type != DbmsType.ClickHouse)
            throw new Exception("Only ClickHouse is supported");
        
        return new ClickHouseContext(_settings.Dbms, _settings.Credentials, _settings.DatabaseName, _settings.Table);
    }
}