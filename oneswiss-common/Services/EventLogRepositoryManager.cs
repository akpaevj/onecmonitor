using OneSwiss.Common.DTO;
using OneSwiss.Common.EventLog;
using OneSwiss.Common.Models;
using OneSwiss.Common.Storage;

namespace OneSwiss.Common.Services;

public class EventLogRepositoryManager
{
    public Func<EventLogSettingsDto, Task>? SettingsChanged = null!;
    public EventLogSettingsDto? Settings { get; private set; }

    public void UpdateSettings(EventLogSettingsDto settings)
    {
        Settings = settings;
        SettingsChanged?.Invoke(Settings);
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